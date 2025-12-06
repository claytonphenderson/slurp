using System.Threading.Channels;
using Models;
using Storage;

namespace Ingestion;

public class IngestionService
{
    private readonly Channel<DataPoint> _channel = Channel.CreateUnbounded<DataPoint>();
    private readonly LocalStorageService _storage;
    public IngestionService(LocalStorageService storage)
    {
        _storage = storage;
        _ = ProcessCollectedDataPoints();
    }

    public async Task IngestNewDataPoint(DataPoint data)
    {
        await _channel.Writer.WriteAsync(data);
    }

    private async Task ProcessCollectedDataPoints()
    {
        await Task.Run(async () =>
        {
            var reader = _channel.Reader;
            await foreach (var item in reader.ReadAllAsync())
            {
                _storage.Save([item]);
            }
        });
    }
}