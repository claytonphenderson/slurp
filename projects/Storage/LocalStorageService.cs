using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace Storage;

public class LocalStorageService
{
    private readonly string _rootPath = "/Volumes/ExternalSSD/slurp-raw";
    private readonly ILogger<LocalStorageService> _logger;
    private readonly Subject<DataPoint> _incomingDataPoints = new ();

    public LocalStorageService(ILogger<LocalStorageService> logger)
    {
        _logger = logger;
        _incomingDataPoints.Buffer(TimeSpan.FromSeconds(5), 1000)
            .Subscribe(objs =>
            {
                var subjectGroups = objs.GroupBy(obj => obj.Subject).ToList();
                subjectGroups.ForEach(group =>
                {
                    var eventGroups = group.GroupBy(obj => obj.Event).ToList();
                    Parallel.ForEachAsync(eventGroups, async (eventGroup, token) =>
                    {
                        // using datetime.utcnow here, but would be best to use the date on the record
                        // in the future to ensure the correct placement in file structure
                        
                        // also need to eventually add a rotation mechanism here to prevent files that are too large
                        var fileName = Utils.GetFileName(group.Key, eventGroup.Key, DateTime.UtcNow);
                        await ExecuteBufferedAppend(fileName, eventGroup.ToList());
                    });
                });
            });
    }

    public void Save(List<DataPoint> objs)
    {
        objs.ForEach(obj => { _incomingDataPoints.OnNext(obj); });
    }

    private async Task ExecuteBufferedAppend(string fileName, List<DataPoint> objs)
    {
        await EnsureFileExists(fileName);
        
        var sb = new StringBuilder();
        var sw = new StreamWriter(File.Open($"{_rootPath}/{fileName}", FileMode.Append));
        try
        {
            objs.ForEach(obj =>
            {
                var str = JsonSerializer.Serialize(obj.Object);
                sb.AppendLine(str);
            });

            await sw.WriteAsync(sb);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error appending to file {fileName}", fileName);
        }
        finally
        {
            await sw.FlushAsync();
            sw.Close();
        }
    }

    private async Task EnsureFileExists(string fileName)
    {
        var directory = Path.GetDirectoryName($"{_rootPath}/{fileName}");
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists($"{_rootPath}/{fileName}"))
        {
            await File.Create($"{_rootPath}/{fileName}").DisposeAsync();
        }
    }
}