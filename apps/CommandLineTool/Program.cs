using System.CommandLine;
using CommandLineTool;
using Microsoft.Extensions.Logging;

var watchOption = new Option<bool>("--watch", "-w");
var fileOption = new Option<string>("--file", "-f");
var subjectOption = new Option<string>("--subject", "-s");

var root = new RootCommand("Slurp CLT \n " +
                           "use flag --watch to continuously read from std in \n" +
                           "use flag --file to specify a file path to read");
root.Options.Add(watchOption);
root.Options.Add(fileOption);
root.Options.Add(subjectOption);

var parseResult = root.Parse(args);
var watchEnabled = parseResult.GetValue(watchOption);
var fileSpecified = parseResult.GetValue(fileOption);
var subject = parseResult.GetValue(subjectOption) ?? "default";

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();
var storageService = new Storage.LocalStorageService(new Logger<Storage.LocalStorageService>(new LoggerFactory()));
var ingestionService = new Ingestion.IngestionService(storageService);

if (watchEnabled)
{
    while (true)
    {
        string? line = await Console.In.ReadLineAsync();
        if (line is null)
        {
            break;
        }

        if (line.Contains("[slurp:"))
        {
            logger.LogInformation(line);

            var dataPoint = StdInParser.Parse(line, subject);
            if (dataPoint is null) continue;

            await ingestionService.IngestNewDataPoint(dataPoint);
        }
    }
}
else if (fileSpecified is not null)
{
    using var sr = new StreamReader(fileSpecified);

    string? line;
    while ((line = await sr.ReadLineAsync()) != null)
    {
        if (line.Contains("[slurp:"))
        {
            logger.LogInformation(line);
            var dataPoint = StdInParser.Parse(line, subject);
            if (dataPoint is null) continue;

            await ingestionService.IngestNewDataPoint(dataPoint);
        }
    }
}

logger.LogInformation("Done.");