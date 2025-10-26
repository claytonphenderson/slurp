using System.Collections.ObjectModel;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Models;

public interface IDbService
{
    Task Insert(string db, string collection, List<JsonElement> objs, bool autoExpire = true);
    Task QuickInsertBatch(IMongoCollection<BsonDocument> collection, List<BsonDocument> docs);
    Task<IMongoCollection<BsonDocument>> CheckCollectionExists(string db, string collection);
}