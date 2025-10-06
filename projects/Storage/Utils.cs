using System.Security.Cryptography;
using System.Text;
using Models;

public static class Utils
{
    public static string GetFileName(string subject, string eventName, DateTime date, string workerId = "0")
    {
        using (var md5 = MD5.Create()) {
        
            return $"{subject}/{eventName}/{date.Year}/{date.ToString("MM")}/{date.ToString("dd")}/{date.ToString("yyyy-MM-dd")}_{workerId}.jsonl";
            // return $"{subject}/{eventName}/{date.Year}/{date.ToString("MM")}/{date.ToString("dd")}/{date.ToString("HH")}/{date.ToString("yyyy-MM-dd:HH")}_{workerId}.jsonl";
        }
    }
}