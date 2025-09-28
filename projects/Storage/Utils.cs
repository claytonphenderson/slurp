using System.Security.Cryptography;
using System.Text;
using Models;

public static class Utils
{
    public static string GetFileName(DataPoint dataPoint, string workerId = "0")
    {
        using (var md5 = MD5.Create()) {
        
            var currentTime = DateTime.UtcNow;
            return $"{dataPoint.Subject}/{dataPoint.Event}/{currentTime.Year}/{currentTime.ToString("MM")}/{currentTime.ToString("dd")}/{currentTime.ToString("HH")}/{currentTime.ToString("yyyy-MM-dd:HH")}_{workerId}.jsonl";
        }
    }
}