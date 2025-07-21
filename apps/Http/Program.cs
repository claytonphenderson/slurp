

using System.Text.Json;
using Ingress;
using Microsoft.AspNetCore.Http.Json;
using Models;
using MongoDB.Driver;
using Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton<IDbService, MongoDbService>();
builder.Services.AddSingleton<DataPointProcessor>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/{subject}/{eventName}/push", async(
    string subject,
    string eventName,
    JsonElement jsonBody,
    DataPointProcessor processor) =>
{
    var dataPoint = new DataPoint
    {
        Subject = subject,
        Event = eventName,
        Object = jsonBody
    };

    await processor.IngestNewDataPoint(dataPoint);
    return Results.Ok();
});

app.Run();