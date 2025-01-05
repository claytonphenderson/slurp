using System.Text.Json;
using DataAccess;
using EventProcessing;
using Host;
using Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHostedService<QueueConsumer>();
DataAccessRegistration.AddDataAccess(builder.Services);
EventProcessingRegistration.AddEventProcessing(builder.Services);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

Console.WriteLine("Api ready");
app.MapPost("/events", async (Event newEvent, HttpContext http) =>
{
    Console.WriteLine($"Received http event {JsonSerializer.Serialize(newEvent)}");
    await app.Services.GetRequiredService<INewEventHandler>().InsertNewEvent(newEvent);
    return Results.Ok();
});

app.Run();