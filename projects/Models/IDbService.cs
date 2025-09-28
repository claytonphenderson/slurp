using System.Text.Json;

namespace Models;

public interface IDbService
{
    Task Insert(string db, string collection, IEnumerable<JsonElement> objs);
}