using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Models;
using MongoDB.Driver;
using Storage;
using StreamReader;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();


builder.Services.AddSingleton<AzureBlobStorageService>();
builder.Services.AddSingleton<DataLakeFileSystemClient>(sp =>
{
    var serviceClient = new DataLakeServiceClient(new Uri("https://slurpdl.blob.core.windows.net/events"), new DefaultAzureCredential());
    return serviceClient.GetFileSystemClient("events");
});

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton<IDbService, MongoDbService>();

var host = builder.Build();
host.Run();
