using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Storage;

public class MongoDbService : IDbService
{
    private readonly IMongoClient _client;
    private readonly ILogger<MongoDbService> _logger;
    private readonly IDictionary<string, List<string>> _dbCollections = new Dictionary<string, List<string>>();
    public MongoDbService(IMongoClient client, ILogger<MongoDbService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Insert(string db, string collection, JsonElement obj)
    {
        try
        {
            // assume the database already exists.  helps prevent unwanted data
            var database = _client.GetDatabase(db);

            // cache the existing collections
            if (!_dbCollections.ContainsKey(db))
            {
                var cursor = await database.ListCollectionNamesAsync();
                _dbCollections[db] = await cursor.ToListAsync();
            }

            // create the new timeseries collection if necessary
            if (!_dbCollections[db].Contains(collection))
            {
                await database.CreateCollectionAsync(collection, new CreateCollectionOptions
                {
                    TimeSeriesOptions = new TimeSeriesOptions("date", "meta", TimeSeriesGranularity.Seconds)
                });
                _logger.LogInformation($"Created new db collection: " + collection);

                _dbCollections[db].Add(collection);
            }

            // insert new record to collection
            var col = database.GetCollection<BsonDocument>(collection);
            _logger.LogDebug(obj.GetRawText());

            // make mongo happy with the date format
            var bson = BsonDocument.Parse(obj.GetRawText());
            bson["date"] = new BsonDateTime(DateTime.UtcNow);

            await col.InsertOneAsync(bson);
            _logger.LogInformation($"Wrote event data to {db} : {collection}");
            
        }
        catch (Exception e)
        {
            _logger.LogError("Could not insert into collection: " + e.Message);
        }
    }
}