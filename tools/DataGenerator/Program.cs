using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Bogus;

await using var producer = new EventHubBufferedProducerClient("https://slurphub.servicebus.windows.net:443/", "events", new DefaultAzureCredential());
var faker = new Faker();

// Start sending events
Console.WriteLine("Sending events...");
int counter = 0;


producer.SendEventBatchFailedAsync += args =>
{
    Console.WriteLine($"Failed to send batch to partition {args.PartitionId}: {args.Exception.Message}");
    return Task.CompletedTask;
};

while (true)
{
    var payload = new
    {
        id = Guid.NewGuid().ToString(),
        ms = faker.Random.Int(),
        date = faker.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ssZ"),
        device = faker.PickRandom(new List<string>() { "Android", "iPhone", "Tablet" }),
        name = faker.Name.FirstName(),
        country = faker.Address.Country(),
        zip = faker.Address.ZipCode(),
        volume = faker.Random.Double(),
        didSignup = faker.Random.Bool(),
        magnitude = faker.Random.Double(),
    };

    var message = new
    {
        Subject = args[0],
        Event = args[1],
        Object = payload
    };

    var eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));

    await producer.EnqueueEventAsync(eventData);

    counter++;
    if (counter % 1000 == 0)
    {
        Console.WriteLine($"Count: {counter}");
    }
}