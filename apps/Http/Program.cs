using System.Reactive.Subjects;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Http;
using Microsoft.AspNetCore.Http.Json;
using Models;
using MongoDB.Driver;
using Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<AzureBlobStorageService>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddSingleton<DataLakeFileSystemClient>(sp =>
{
    var serviceClient = new DataLakeServiceClient(new Uri("https://slurpdl.blob.core.windows.net"), new DefaultAzureCredential());
    return serviceClient.GetFileSystemClient("events");
});

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton<IDbService, MongoDbService>();
builder.Services.AddSingleton<DataPointProcessor>();
builder.Services.AddSingleton<Subject<DataPoint>>(_ => new Subject<DataPoint>());

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/{subject}/{eventName}/push", async (
    string subject,
    string eventName,
    JsonElement jsonBody,
    DataPointProcessor processor) =>
{
    try
    {
        var dataPoint = new DataPoint
        {
            Subject = subject,
            Event = eventName,
            Object = jsonBody,
        };

        await processor.IngestNewDataPoint(dataPoint);
        return Results.Ok();
    }
    catch (Exception e)
    {
        return Results.InternalServerError("A Problem occurred while submitting new datapoint");
    }
});

app.MapPost("/{subject}/{eventName}/load", async (
    string subject,
    string eventName,
    ColdFetchRequest request,
    IStorageService storage,
    IDbService db) =>
    {
        try
        {
            await storage.FetchColdData(subject, eventName, request.Start, request.End, db);
            return Results.Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            return Results.InternalServerError("A problem occurred while loading cold data");
        }
    });

app.Run();

record ColdFetchRequest {
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
};