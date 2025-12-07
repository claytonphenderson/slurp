using System.Text.Json;

namespace Models;

public record DataPoint
{
    public required string Subject { get; set; }
    public required string Event { get; set; }
    public DateTime? Date { get; set; }
    public required JsonElement Object { get; set; }
}