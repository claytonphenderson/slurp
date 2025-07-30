using System.Text.Json;

namespace Models;

public record class DataPoint
{
    public string Id = Guid.NewGuid().ToString();
    public required string Subject;
    public required string Event;
    public required JsonElement Object;
}