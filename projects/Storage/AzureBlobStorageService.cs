using System.Text.Json;
using System.Text;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.DataLake.Models;
using Models;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Storage;

/// <summary>
/// TODO:
/// - create logic to break into smaller files if a single file grows larger than some threshold
/// - write errors to a dead letter queue
/// </summary>
public class AzureBlobStorageService
{
    private readonly DataLakeFileSystemClient _fileSystemClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private static readonly SemaphoreSlim _writeLock = new(1, 1);
    public AzureBlobStorageService(DataLakeFileSystemClient fileSystemClient, ILogger<AzureBlobStorageService> logger)
    {
        _fileSystemClient = fileSystemClient;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if successful
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="objs"></param>
    /// <returns></returns>
    public async Task AppendToFile(string fileName, List<JsonElement> objs)
    {
        await _writeLock.WaitAsync();

        try
        {
            var fileClient = _fileSystemClient.GetFileClient(fileName);
            await fileClient.CreateIfNotExistsAsync();

            var fullStr = String.Empty;
            objs.ForEach(obj =>
            {
                var str = JsonSerializer.Serialize(obj);
                fullStr += str + "\n";
            });

            var bytes = Encoding.UTF8.GetBytes(fullStr);
            var currentSize = (await fileClient.GetPropertiesAsync()).Value.ContentLength;
            using (var stream = new MemoryStream(bytes))
            {
                await fileClient.AppendAsync(stream, currentSize);
            }

            await fileClient.FlushAsync(currentSize + bytes.Length);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not append to blob storage");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task FetchColdData(string subject, string eventName, DateTime start, DateTime end, Subject<DataPoint> ingestSubject)
    {
        var paths = new List<PathItem>();
        foreach (var dir in GenerateDateDirectories(subject, eventName, start, end))
        {
            await foreach (var pathItem in _fileSystemClient.GetPathsAsync(path: dir, recursive: false))
            {
                // Only include files (exclude subdirectories)
                if (!pathItem.IsDirectory.HasValue || pathItem.IsDirectory == false)
                {
                    paths.Add(pathItem);
                }
            }
        }

        Parallel.ForEach(paths, async path =>
        {
            var fileClient = _fileSystemClient.GetFileClient(path.Name);
            var content = await fileClient.ReadStreamingAsync();
            using var reader = new StreamReader(content.Value.Content);

            while (!reader.EndOfStream)
            {
                var json = await reader.ReadLineAsync();
                if (String.IsNullOrEmpty(json)) continue;
                var dataPoint = JsonSerializer.Deserialize<DataPoint>(json);
                if (dataPoint is null)
                {
                    _logger.LogWarning("Could not deserialize data point... skipping");
                    continue;
                }

                ingestSubject.OnNext(dataPoint);
            }
        });
    }

    private IEnumerable<string> GenerateDateDirectories(string subject, string eventName, DateTime start, DateTime end)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            // yyyy/MM/dd
            yield return $"{subject}/{eventName}{date:yyyy/MM/dd}";
        }
    }
}