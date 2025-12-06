namespace Storage;

public static class Utils
{
    public static string GetFileName(string subject, string eventName, DateTime date, int inc = 0)
    {
        return
            $"{subject}/{eventName}/{date.Year}/{date.ToString("MM")}/{date.ToString("dd")}/{date.ToString("yyyy-MM-dd")}_{inc}.jsonl";
    }

    public static IEnumerable<string> GenerateDateDirectories(string subject, string eventName, DateTime start,
        DateTime end)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddMonths(1))
        {
            yield return $"/Volumes/ExternalSSD/slurp-raw/{subject}/{eventName}/{date:yyyy/MM}";
        }
    }
}