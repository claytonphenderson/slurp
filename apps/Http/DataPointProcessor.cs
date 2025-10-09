using System.Threading.Channels;
using Models;
using Storage;

namespace Http;

/// <summary>
/// Think the channel is overkill, huh? Well it allows the api call to return in 
/// less than 10ms so I think I'll stick with it.
/// 
/// TODO:
/// - make the append to file and mongo save return results obj so we can handle errors
/// </summary>
public class DataPointProcessor
{
    private readonly Channel<DataPoint> channel = Channel.CreateUnbounded<DataPoint>();
    private readonly IDbService _db;
    private readonly AzureBlobStorageService _blob;
    public DataPointProcessor(IDbService db, AzureBlobStorageService blob)
    {
        _db = db;
        _blob = blob;

        _ = ProcessCollectedDataPoints();
    }

    public async Task IngestNewDataPoint(DataPoint data)
    {
        await channel.Writer.WriteAsync(data);
    }

    private async Task ProcessCollectedDataPoints()
    {
        await Task.Run(async () =>
        {
            var reader = channel.Reader;
            await foreach (var item in reader.ReadAllAsync())
            {
                var date = DateTime.Parse(item.Object.GetProperty("date").ToString());
                Task.WaitAll([
                    _blob.AppendToFile(Utils.GetFileName(item.Subject, item.Event, date, "restapi"), [item.Object]),
                    _db.Insert(item.Subject, item.Event, [item.Object])
                ]);
            }
        });
    }
}