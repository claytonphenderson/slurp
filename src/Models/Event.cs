using System.Text.Json.Serialization;

namespace Models;

public class Event
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    [JsonPropertyName("eventName")]
    public required string EventName { get; set; }
    [JsonPropertyName("properties")]
    public required object Properties { get; set; }
}