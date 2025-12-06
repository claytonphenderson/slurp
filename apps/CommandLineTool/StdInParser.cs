using System.Text.Json;
using Models;

namespace CommandLineTool;

public static class StdInParser
{
    public static DataPoint? Parse(string line, string subject = "default")
    {
        try
        {
            line = line.Replace(" ", "");
            var startIndex = line.IndexOf("[slurp:");
            var sub = line.Split("[slurp:");
            var eventNameLength = sub[1].IndexOf(']');
            var eventName = line.Substring(startIndex + "[slurp:".Length, eventNameLength).Trim();
            var record = line.Substring(eventNameLength + 1 + startIndex + "[slurp:".Length).Trim();
            var jsonElement = JsonDocument.Parse(record);
            var date = jsonElement.RootElement.TryGetProperty("date", out var dateElement)
                ? dateElement.GetDateTime()
                : DateTime.UtcNow;

            return new DataPoint()
            {
                Subject = subject,
                Event = eventName,
                Object = jsonElement.RootElement,
                Date = date
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Could not parse record: {line}" + e);
        }

        return null;
    }
}