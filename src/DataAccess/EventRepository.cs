using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Models;
using Npgsql;

namespace DataAccess;

public class EventRepository : IEventRepository
{
    private readonly NpgsqlConnection _connection;
    public EventRepository(IConfiguration configuration)
    {
        _connection = new NpgsqlConnection(configuration["Database:ConnectionString"]);
        _connection.Open();
    }

    public async Task Insert(Event newEvent)
    {
        try
        {
            await using (var cmd =
            new NpgsqlCommand("INSERT INTO events (id, date, app, environment, eventname, properties, inserteddate) VALUES (@i, @d, @a, @en, @e, @p, @id)", _connection))
            {
                cmd.Parameters.AddWithValue("@i", newEvent.Id);
                cmd.Parameters.AddWithValue("@d", newEvent.Date);
                cmd.Parameters.AddWithValue("@a", newEvent.App != null ? newEvent.App : DBNull.Value);
                cmd.Parameters.AddWithValue("@en", newEvent.Environment != null ? newEvent.Environment : "NULL");
                cmd.Parameters.AddWithValue("@e", newEvent.EventName);
                cmd.Parameters.AddWithValue("@p", newEvent.Properties);
                cmd.Parameters.AddWithValue("@id", DateTimeOffset.UtcNow);

                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error trying to insert new event to events table " + e.Message);
        }
    }
}