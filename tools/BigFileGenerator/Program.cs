using System.Text.Json;
using Bogus;


var countToCreate = int.Parse(args[0]); // number of fake data points
var destination = args[1]; // where to write the datapoints

// if (!File.Exists(destination))
// {
//     File.Create(destination);
// }


var faker = new Faker();
var createdCount = 0;


var batch = "";
var batchCount = 0;
while (createdCount < countToCreate)
{
    // var dateBsonJson = "{ \"$date\": \"" +
    // faker.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now)
    //      .ToString("yyyy-MM-ddTHH:mm:ssZ") +
    // "\" }";

    var deviceString = faker.PickRandom(new List<string>() { "Android", "iPhone", "Tablet" });

    var payload = new
    {
        id = Guid.NewGuid().ToString(),
        ms = faker.Random.Int(),
        date = faker.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now),
        meta = new
        {
            device = deviceString
        },
        device = faker.PickRandom(new List<string>() { "Android", "iPhone", "Tablet" }),
        name = faker.Name.FirstName(),
        country = faker.Address.Country(),
        zip = faker.Address.ZipCode(),
        volume = faker.Random.Double(),
        didSignup = faker.Random.Bool(),
        magnitude = faker.Random.Double(),
    };
    batch += JsonSerializer.Serialize(payload) + "\n";
    createdCount++;
    batchCount++;
    if (batchCount >= 10_000)
    {
        Console.WriteLine("Writing Batch... " + createdCount);
        File.AppendAllText(destination, batch);
        batch = "";
        batchCount = 0;
    }
}

if (batchCount > 0)
{
    File.AppendAllText(destination, batch);
}

Console.WriteLine("Done");
Console.Beep();