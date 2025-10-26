using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Bson;

namespace Storage;

public class LocalStorageService : IStorageService
{
    private readonly string rootPath = "/Volumes/ExternalSSD/slurp-raw";
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(ILogger<LocalStorageService> logger)
    {
        _logger = logger;
    }

    public async Task AppendToFile(string fileName, List<JsonElement> objs)
    {
        var fileLock = locks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync();
        try
        {
            var fullStr = String.Empty;
            objs.ForEach(obj =>
            {
                var str = JsonSerializer.Serialize(obj);
                fullStr += str + "\n";
            });

            var directory = Path.GetDirectoryName($"{rootPath}/{fileName}");
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.AppendAllTextAsync($"{rootPath}/{fileName}", fullStr);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error appending to file");
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task FetchColdData(string subject, string eventName, DateTime start, DateTime end, IDbService db)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            var paths = new List<string>();
            foreach (var dir in GenerateDateDirectories(subject, eventName, start, end))
            {
                var allFiles = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Where(f=>!Path.GetFileName(f).StartsWith("._")).ToList();
                paths.AddRange(allFiles);
            }
            _logger.LogInformation($"Loading {paths.Count} paths...");
            var collection = await db.CheckCollectionExists(subject, $"{eventName}_{start.ToUniversalTime().ToString("yyyy-MM")}_{end.ToUniversalTime().ToString("yyyy-MM")}");

            await Parallel.ForEachAsync(paths, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 4
            }, async (path, ct) =>
            {
                try
                {
                    var fileBatch = new List<BsonDocument>();

                    foreach (var line in File.ReadLines(path)
                        .Where(l => !string.IsNullOrWhiteSpace(l) && l.All(c => !char.IsControl(c) || c == '\n' || c == '\r')))
                    {
                        try
                        {
                            var bson = BsonDocument.Parse(line);
                            var parsedDate = bson["date"].AsString;
                            bson["date"] = new BsonDateTime(DateTime.Parse(parsedDate));
                            bson["meta"] = new BsonDocument();

                            fileBatch.Add(bson);
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "deserializaion at " + line.ToString());
                        }

                    }
                    _logger.LogInformation($"Writing {fileBatch.Count} documents from {path}");
                    // await db.Insert(subject, $"{eventName}_{start.ToUniversalTime().ToString("yyyy-MM")}_{end.ToUniversalTime().ToString("yyyy-MM")}", fileBatch.Select(x => x.Object).ToList(), false);
                    await db.QuickInsertBatch(collection, fileBatch);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error writing cold data to mongo");
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
            yield return $"/Volumes/ExternalSSD/slurp-raw/{subject}/{eventName}/{date:yyyy/MM}";
        }
    }
}