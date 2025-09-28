using System.Text.Json;
using System.Text;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.Logging;

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
    public async Task<bool> AppendToFile(string fileName, IEnumerable<JsonElement> objs)
    {
        try
        {
            var fileClient = _fileSystemClient.GetFileClient(fileName);
            await fileClient.CreateIfNotExistsAsync();

            var fullStr = String.Empty;
            foreach (var obj in objs)
            {
                var str = JsonSerializer.Serialize(obj);
                fullStr += str + "\n";
            }

            var bytes = Encoding.UTF8.GetBytes(fullStr);
            var currentSize = (await fileClient.GetPropertiesAsync()).Value.ContentLength;
            using (var stream = new MemoryStream(bytes))
            {
                await fileClient.AppendAsync(stream, currentSize);
            }

            await fileClient.FlushAsync(currentSize + bytes.Length);

            _logger.LogInformation($"Appended {bytes.Length} bytes to {fileName}");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not append to blob storage");
            return false;
        }
    }
}