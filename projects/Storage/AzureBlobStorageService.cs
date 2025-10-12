using System.Text.Json;
using System.Text;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.DataLake.Models;
using Models;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Diagnostics;

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
        try
        {
            var sw = Stopwatch.StartNew();

            var paths = new List<PathItem>();
            foreach (var dir in GenerateDateDirectories(subject, eventName, start, end))
            {
                await foreach (var pathItem in _fileSystemClient.GetPathsAsync(dir, true))
                {
                    // Only include files (exclude subdirectories)
                    if (!pathItem.IsDirectory.HasValue || pathItem.IsDirectory == false)
                    {
                        paths.Add(pathItem);
                    }
                }
            }
            _logger.LogInformation($"Loading {paths.Count} paths...");
            Parallel.ForEach(paths, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 10
            }, async path =>
            {
                var fileClient = _fileSystemClient.GetFileClient(path.Name);
                var content = await fileClient.ReadStreamingAsync();
                using var reader = new StreamReader(content.Value.Content);

                while (!reader.EndOfStream)
                {
                    var json = await reader.ReadLineAsync();
                    if (String.IsNullOrEmpty(json)) continue;
                    var payload = JsonSerializer.Deserialize<JsonElement>(json);
                    var dataPoint = new DataPoint()
                    {
                        Subject = subject,
                        Event = $"{eventName}_{start.ToUniversalTime().ToString("yyyy-MM")}_{end.ToUniversalTime().ToString("yyyy-MM")}",
                        Object = payload,
                        SkipBlobUpload = true,
                        IsLiveCapture = false
                    };
                    ingestSubject.OnNext(dataPoint);
                }
            });
            sw.Stop();
            _logger.LogInformation($"Loaded {paths.Count} files in {sw.Elapsed.TotalSeconds} seconds");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading from cold storage");
        }
    }

    private IEnumerable<string> GenerateDateDirectories(string subject, string eventName, DateTime start, DateTime end)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddMonths(1))
        {
            yield return $"{subject}/{eventName}/{date:yyyy/MM}";
        }
    }
}