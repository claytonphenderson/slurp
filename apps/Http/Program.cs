using System.Reactive.Subjects;
using System.Text.Json;
using Ingestion;
using Microsoft.AspNetCore.Http.Json;
using Models;
using Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<LocalStorageService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<LocalStorageService>>();
    var dataDir = config.GetValue<string>("SlurpDataDirectoryPath") ?? "/tmp/slurp-data";
    return new LocalStorageService(dataDir, logger);});

builder.Services.AddSingleton<IngestionService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/{subject}/{eventName}/push", async (
    string subject,
    string eventName,
    List<JsonElement> jsonBody,
    IngestionService processor) =>
{
    try
    {
        jsonBody.ForEach(async obj =>
        {
            var dataPoint = new DataPoint
            {
                Subject = subject,
                Event = eventName,
                Object = obj,
            };

            await processor.IngestNewDataPoint(dataPoint);
        });

        return Results.Ok();
    }
    catch (Exception e)
    {
        return Results.InternalServerError("A Problem occurred while submitting new datapoint");
    }
});

app.Run();