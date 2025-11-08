using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Storage;

/// <summary>
/// Todo:
/// - write errors to dead letter queue
/// - figure out how to handle deduplication 
/// </summary>
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

    public async Task Insert(string db, string collection, List<JsonElement> objs, bool autoExpire = true)
    {
        var sw = new Stopwatch();
        try
        {
            if (objs.Count == 0) return;

            // assume the database already exists.  helps prevent unwanted data
            var database = _client.GetDatabase(db);
            if (database is null)
            {
                throw new Exception($"Subject {db} has no existing mongo database");
            }

            // cache the existing collections
            if (!_dbCollections.ContainsKey(db))
            {
                var cursor = await database.ListCollectionNamesAsync();
                _dbCollections[db] = await cursor.ToListAsync();
            }

            // create the new timeseries collection if necessary
            if (!_dbCollections[db].Contains(collection))
            {
                if (autoExpire)
                {
                    await database.CreateCollectionAsync(collection, new CreateCollectionOptions
                    {
                        TimeSeriesOptions = new TimeSeriesOptions("date", "meta", TimeSeriesGranularity.Hours),
                        // This is based on the "date" field on the document!
                        ExpireAfter = TimeSpan.FromDays(14)
                    });
                }
                else
                {
                    await database.CreateCollectionAsync(collection, new CreateCollectionOptions
                    {
                        TimeSeriesOptions = new TimeSeriesOptions("date", "meta", TimeSeriesGranularity.Hours),
                    });
                }

                _logger.LogInformation($"Created new db collection: " + collection);
                _dbCollections[db].Add(collection);
            }

            // insert new records to collection
            var col = database.GetCollection<BsonDocument>(collection);
            var bsonDocs = new List<BsonDocument>();
            objs.ForEach(obj =>
            {
                // make mongo happy with the date format
                var bson = BsonDocument.Parse(obj.GetRawText());
                bson["date"] = new BsonDateTime(DateTime.UtcNow);

                // try to parse the date from the json input
                if (obj.TryGetProperty("date", out JsonElement dateElement))
                {
                    if (dateElement.TryGetDateTime(out DateTime dateTime))
                    {
                        bson["date"] = new BsonDateTime(dateTime);
                    }
                }

                bson["meta"] = new BsonDocument();

                // set the _id field here to the provided guid.  Mongo timeseries doesnt allow
                // for uniqueness constraints, so this is ultimately something that will help in 
                // post processing to eliminate duplicates
                bson["_id"] = bson["id"];
                bson.Remove("id");

                bsonDocs.Add(bson);
            });

            sw.Start();
            await col.InsertManyAsync(bsonDocs, new InsertManyOptions
            {
                IsOrdered = false
            });
            sw.Stop();
            _logger.LogInformation($"Wrote {bsonDocs.Count} documents to {db}.{collection} in {sw.Elapsed.TotalSeconds}s");

        }
        catch (Exception e)
        {
            _logger.LogError("Could not insert into collection: " + e.Message);
        }
        finally
        {
            sw.Stop();
        }
    }

    public async Task QuickInsertBatch(IMongoCollection<BsonDocument>col, List<BsonDocument> docs)
    {
        if (docs.Count == 0) return;

        await col.InsertManyAsync(docs, new InsertManyOptions
        {
            IsOrdered = false
        });
        // _logger.LogInformation($"Wrote {docs.Count} documents to {col.CollectionNamespace}");

    }
    
    public async Task<IMongoCollection<BsonDocument>> CheckCollectionExists(string db, string collection)
    {
        // assume the database already exists.  helps prevent unwanted data
        var database = _client.GetDatabase(db);
        if (database is null)
        {
            throw new Exception($"Subject {db} has no existing mongo database");
        }

        var cursor = await database.ListCollectionNamesAsync();
        _dbCollections[db] = await cursor.ToListAsync();

        // create the new timeseries collection if necessary
        if (!_dbCollections[db].Contains(collection))
        {

            await database.CreateCollectionAsync(collection, new CreateCollectionOptions
            {
                TimeSeriesOptions = new TimeSeriesOptions("date", "meta", TimeSeriesGranularity.Hours),
            });


            _logger.LogInformation($"Created new db collection: " + collection);
            _dbCollections[db].Add(collection);
        }
        return database.GetCollection<BsonDocument>(collection);
    }
}