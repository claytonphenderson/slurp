using System.Text.Json;

namespace Models;

public record class DataPoint
{
    public required string Subject { get; set; }
    public required string Event { get; set; }
    public DateTime? Date { get; set; }
    public required JsonElement Object { get; set; }
    public bool SkipBlobUpload { get; set; } = false;
}