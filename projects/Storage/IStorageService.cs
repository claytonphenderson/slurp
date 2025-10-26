using System.Text.Json;
using Models;

namespace Storage;

public interface IStorageService
{
    Task AppendToFile(string fileName, List<JsonElement> objs);
    Task FetchColdData(string subject, string eventName, DateTime start, DateTime end, IDbService db);
}