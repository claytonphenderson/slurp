using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Models;

namespace Ingress;

public class DataPointProcessor
{
    private readonly Channel<DataPoint> channel = Channel.CreateUnbounded<DataPoint>();
    private readonly ILogger<DataPointProcessor> _logger;
    private readonly IDbService _db;
    public DataPointProcessor(ILogger<DataPointProcessor> logger, IDbService db)
    {
        _logger = logger;
        _db = db;

        _ = ProcessCollectedDataPoints();
    }

    public async Task IngestNewDataPoint(DataPoint data)
    {
        if (!data.Object.TryGetProperty("meta", out _) || !data.Object.TryGetProperty("date", out _))
        {
            throw new Exception("Invalid request properties");
        }
        
        await channel.Writer.WriteAsync(data);
    }

    private async Task ProcessCollectedDataPoints()
    {
        await Task.Run(async () =>
        {
            var reader = channel.Reader;
            await foreach (var item in reader.ReadAllAsync())
            {
                await _db.Insert(item.Subject, item.Event, item.Object);
            }
        });
    }
}