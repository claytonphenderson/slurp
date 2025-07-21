using System.Text.Json;

namespace Models;

public record class DataPoint
{
    public required string Subject;
    public required string Event;
    public required JsonElement Object;
}