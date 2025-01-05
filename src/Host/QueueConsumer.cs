
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Queues;
using EventProcessing;
using Models;

namespace Host;

public class QueueConsumer : BackgroundService
{
    private readonly QueueClient _queueClient;
    private readonly INewEventHandler _eventHandler;

    public QueueConsumer(INewEventHandler eventHandler, IConfiguration configuration)
    {
        _queueClient = new QueueClient(new Uri(configuration["Queue:Uri"]), new DefaultAzureCredential());
        _eventHandler = eventHandler;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Starting queue consumer");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queueClient.ReceiveMessageAsync();
                if (message.Value == null)
                {
                    Console.WriteLine("No messages available. Waiting 60s...", DateTimeOffset.UtcNow);
                    await Task.Delay(60_000);
                    continue;
                }

                var bodyMessage = Convert.FromBase64String(message.Value.Body.ToString());
                var newEvent = JsonSerializer.Deserialize<Event>(Encoding.UTF8.GetString(bodyMessage));
                if (newEvent == null)
                {
                    Console.WriteLine("Could not deserialize incoming event", message.Value.Body.ToString());
                    continue;
                }

                await _eventHandler.InsertNewEvent(newEvent);
                await _queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
                Console.WriteLine("Consumed queued event");
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught error in queue consumer " + e.Message);
            }

        }
        return;
    }
}