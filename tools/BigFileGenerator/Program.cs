using System.Text;
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


var batchCount = 0;
var sw = new StreamWriter(File.Create(destination));
var sb = new StringBuilder();
while (createdCount < countToCreate)
{
    var dateString = faker.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now)
         .ToString("yyyy-MM-ddTHH:mm:ssZ");

    var deviceString = faker.PickRandom(new List<string>() { "Android", "iPhone", "Tablet" });
    var countryString = faker.Address.Country();
    var payload = new
    {
        id = Guid.NewGuid().ToString(),
        ms = faker.Random.Int(),
        // date = new Dictionary<string, string>()
        // {
        //     {"$date", dateString}
        // },
        date = dateString,
        meta = new
        {
            device = deviceString,
            country = countryString
        },
        device = deviceString,
        name = faker.Name.FirstName(),
        country = countryString,
        zip = faker.Address.ZipCode(),
        volume = faker.Random.Double(),
        didSignup = faker.Random.Bool(),
        magnitude = faker.Random.Double(),
        nested = new
        {
            secret = faker.Random.Word(),
            isCool = faker.Random.Bool(),
        },
        arr = faker.Random.WordsArray(3)
    };
    sb.AppendLine(JsonSerializer.Serialize(payload));
    createdCount++;
    batchCount++;
    if (batchCount >= 10_000)
    {
        Console.WriteLine("Writing Batch... " + createdCount);
        // File.AppendAllText(destination, batch);
        await sw.WriteAsync(sb);
        sb.Clear();
        batchCount = 0;
    }
}

if (batchCount > 0)
{
    await sw.WriteLineAsync(sb);
}

sw.Flush();
sw.Close();

Console.WriteLine("Done");
Console.Beep();