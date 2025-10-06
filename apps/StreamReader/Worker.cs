using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Models;
using Storage;

namespace StreamReader;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly AzureBlobStorageService _blob;
    private readonly IDbService _db;


    public Worker(ILogger<Worker> logger, IDbService db, AzureBlobStorageService blob)
    {
        _logger = logger;
        _db = db;
        _blob = blob;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Checkpoint blob container
        var blobContainerClient = new BlobContainerClient(new Uri("https://slurpcheckpoints.blob.core.windows.net/eventscheckpoints"), new DefaultAzureCredential());
        await blobContainerClient.CreateIfNotExistsAsync();

        string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
        var processor = new EventProcessorClient(blobContainerClient, consumerGroup, "https://slurphub.servicebus.windows.net:443/", "events", new DefaultAzureCredential());
        var dataPointsSubject = new Subject<DataPoint>();
        var bufferedDataPoints = dataPointsSubject
            .Buffer(TimeSpan.FromSeconds(5), 10000);
        ProcessEventArgs? lastProcessedArgs = null;

        // TODO: handle shutdown gracefully to let buffer flush
        // TODO: failure handling when mongo or file append fails
        bufferedDataPoints
            .Subscribe(dataPoints =>
            {
                _logger.LogInformation($"Batch size: {dataPoints.Count}");

                // First group by subject and event type
                var grouped = dataPoints.GroupBy(x => $"{x.Subject}:{x.Event}").ToList();
                grouped.ForEach(async g =>
                {
                    // bulk insert to mongo collection
                    await _db.Insert(g.First().Subject, g.First().Event, g.Select(x => x.Object).ToList());

                    // Then group events by day of "date" field
                    var dateGroup = g.ToList()
                        .Where(x => x.Date != null)
                        .GroupBy(x => new DateTime(
                            x.Date.Value.Year,
                            x.Date.Value.Month,
                            x.Date.Value.Day,
                            0,
                            0,
                            0))
                        .ToList();

                    dateGroup.ForEach(async d =>
                    {
                        // bulk append these events to their appropriate file
                        await _blob.AppendToFile(Utils.GetFileName(d.First().Subject, d.First().Event, d.First().Date ?? DateTime.UtcNow, processor.Identifier), d.Select(x => x.Object).ToList());
                    });
                    _logger.LogInformation($"Appended to {dateGroup.Count} files");
                });
            });

        var processedCount = 0;
        processor.ProcessEventAsync += async (args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Data.EventBody);
                var item = JsonSerializer.Deserialize<DataPoint>(body);
                if (item is null)
                {
                    _logger.LogWarning("Null item detected.  Skipping");
                    return;
                }

                item.Date = DateTime.Parse(item.Object.GetProperty("date").ToString()).ToUniversalTime();

                dataPointsSubject.OnNext(item);
                lastProcessedArgs = args;
                processedCount++;
                if (processedCount > 1000)
                {
                    await args.UpdateCheckpointAsync(args.CancellationToken);
                    // _logger.LogInformation("Updated checkpoint");
                    processedCount = 0;
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Task was cancelled.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error when handling new message");
            }
        };

        // TODO: some kind of dead letter queuing needed here
        processor.ProcessErrorAsync += async (args) =>
        {
            _logger.LogError($"Error in {args.PartitionId}: {args.Exception.Message}");
            await Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);

        stoppingToken.WaitHandle.WaitOne();

        await processor.StopProcessingAsync();
        if (lastProcessedArgs.HasValue) await lastProcessedArgs.Value.UpdateCheckpointAsync();

        // wait for buffer to flush... this still isnt a perfect approach, but will do for now
        await bufferedDataPoints.Where(x => x.Count == 0).FirstAsync().ToTask();

        _logger.LogInformation("Updated checkpoint and flushed buffer.  Bye.");
    }
}
