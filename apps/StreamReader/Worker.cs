using System.Text;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;

namespace StreamReader;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

        // Checkpoints
        var blobContainerClient = new BlobContainerClient(new Uri("https://slurpcheckpoints.blob.core.windows.net/eventscheckpoints"), new DefaultAzureCredential());
        await blobContainerClient.CreateIfNotExistsAsync();


        var processor = new EventProcessorClient(blobContainerClient, consumerGroup, "https://slurphub.servicebus.windows.net:443/", "events", new DefaultAzureCredential());
        processor.ProcessEventAsync += async (args) =>
        {
            var body = Encoding.UTF8.GetString(args.Data.EventBody);
            Console.WriteLine($"Partition {args.Partition.PartitionId}: {body}");

            // Automatic checkpointing
            await args.UpdateCheckpointAsync(args.CancellationToken);
        };

        processor.ProcessErrorAsync += async (args) =>
        {
            Console.WriteLine($"Error in {args.PartitionId}: {args.Exception.Message}");
            await Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);

        await processor.StopProcessingAsync();
    }
}
