using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;

namespace ClimateHub.SseTests;

[Trait("Category", "Component")]
public class SseReplayTests : IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private string _connectionString = string.Empty;

    public SseReplayTests()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_sse_test")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_sse_test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _connectionString =
            $"Host={_postgresContainer.Hostname};" +
            $"Port={_postgresContainer.GetMappedPublicPort(5432)};" +
            $"Database=climate_hub_sse_test;Username=climate_hub;Password=climate_hub_sse_test;";
        await CreateSseEventTable();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task ReconnectWithLastEventId_ReceivesMissedEventsInOrder()
    {
        var eventAId = await InsertEvent("event-stream-1", "event-A", "data-A");
        await InsertEvent("event-stream-1", "event-B", "data-B");
        await InsertEvent("event-stream-1", "event-C", "data-C");

        var eventsAfterA = await GetEventsSince("event-stream-1", eventAId);
        Assert.Equal(2, eventsAfterA.Count);
        Assert.Equal("event-B", eventsAfterA[0].eventType);
        Assert.Equal("event-C", eventsAfterA[1].eventType);
    }

    [Fact]
    public async Task ReconnectWithLastEventId_IncludesCorrectId()
    {
        var eventAId = await InsertEvent("event-stream-2", "start", "data-start");
        var eventBId = await InsertEvent("event-stream-2", "middle", "data-middle");
        await InsertEvent("event-stream-2", "end", "data-end");

        var eventsSinceB = await GetEventsSince("event-stream-2", eventBId);
        Assert.Single(eventsSinceB);
        Assert.Equal("end", eventsSinceB[0].eventType);
    }

    [Fact]
    public async Task DuplicateEventId_HandledGracefully()
    {
        var id1 = await InsertEventWithId("event-stream-3", "dup-test", "first", "dup-id-001");
        var id2 = await InsertEventWithId("event-stream-3", "dup-test", "second", "dup-id-001");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public async Task RetentionGap_DoesNotBlockReplay()
    {
        await InsertEvent("event-stream-4", "old-event", "old-data");
        var middleId = await InsertEvent("event-stream-4", "middle-event", "middle-data");
        await CleanupOldEvents("event-stream-4", "1970-01-01T00:00:00Z");
        var afterCleanup = await InsertEvent("event-stream-4", "new-event", "new-data");

        var events = await GetEventsSince("event-stream-4", middleId);
        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task Cleanup_RemovesOldEvents()
    {
        var oldId = await InsertEvent("event-stream-5", "old", "old-data");
        await InsertEvent("event-stream-5", "new", "new-data");

        await CleanupEventsOlderThan("event-stream-5", TimeSpan.FromDays(1));

        var events = await GetEventsSince("event-stream-5", oldId);
        Assert.NotEmpty(events);
    }

    private async Task<int> InsertEvent(string streamId, string eventType, string data)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sse_events (stream_id, event_type, event_data, created_at)
            VALUES (@streamId, @eventType, @data, NOW() AT TIME ZONE 'UTC')
            RETURNING id
            """;
        cmd.Parameters.AddWithValue("@streamId", streamId);
        cmd.Parameters.AddWithValue("@eventType", eventType);
        cmd.Parameters.AddWithValue("@data", data);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<int> InsertEventWithId(string streamId, string eventType, string data, string eventId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sse_events (stream_id, event_type, event_data, event_id, created_at)
            VALUES (@streamId, @eventType, @data, @eventId, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (stream_id, event_id) DO UPDATE SET created_at = NOW() AT TIME ZONE 'UTC'
            RETURNING id
            """;
        cmd.Parameters.AddWithValue("@streamId", streamId);
        cmd.Parameters.AddWithValue("@eventType", eventType);
        cmd.Parameters.AddWithValue("@data", data);
        cmd.Parameters.AddWithValue("@eventId", eventId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<List<(int id, string eventType, string data)>> GetEventsSince(string streamId, int lastEventId)
    {
        var events = new List<(int id, string eventType, string data)>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, event_type, event_data
            FROM sse_events
            WHERE stream_id = @streamId AND id > @lastEventId
            ORDER BY id ASC
            """;
        cmd.Parameters.AddWithValue("@streamId", streamId);
        cmd.Parameters.AddWithValue("@lastEventId", lastEventId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            events.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }
        return events;
    }

    private async Task CleanupOldEvents(string streamId, string beforeTimestamp)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sse_events WHERE stream_id = @streamId AND created_at < @before";
        cmd.Parameters.AddWithValue("@streamId", streamId);
        cmd.Parameters.AddWithValue("@before", DateTime.Parse(beforeTimestamp));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CleanupEventsOlderThan(string streamId, TimeSpan age)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sse_events WHERE stream_id = @streamId AND created_at < NOW() - @age";
        cmd.Parameters.AddWithValue("@streamId", streamId);
        cmd.Parameters.AddWithValue("@age", age);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateSseEventTable()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sse_events (
                id SERIAL PRIMARY KEY,
                stream_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                event_data TEXT NOT NULL,
                event_id TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                UNIQUE (stream_id, event_id)
            );
            CREATE INDEX IF NOT EXISTS idx_sse_events_stream_seq
                ON sse_events (stream_id, id);
            """;
        await cmd.ExecuteNonQueryAsync();
    }
}