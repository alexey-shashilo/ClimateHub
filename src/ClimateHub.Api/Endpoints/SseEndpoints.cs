using System.Text.Json;
using ClimateHub.Infrastructure.Events;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class SseEndpoints
{
    public static void MapSseEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/events/environment/{roomId}", async (
            string roomId,
            EnvironmentEventBus eventBus,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid))
                return Results.BadRequest(new { code = "INVALID_ROOM_ID" });

            var rid = RoomId.From(guid);
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            using var writer = new StreamWriter(context.Response.Body) { AutoFlush = false };

            void OnUpdated(object? sender, EnvironmentUpdatedEvent evt)
            {
                if (evt.RoomId != rid) return;
                try
                {
                    var sseEventType = evt.EventType switch
                    {
                        "environment.updated" or "environment.state.changed" => "environment.updated",
                        _ => evt.EventType
                    };
                    var payloadDict = new Dictionary<string, object?>
                    {
                        ["type"] = evt.EventType,
                        ["roomId"] = evt.RoomId.ToString(),
                        ["timestamp"] = evt.Timestamp,
                        ["correlationId"] = evt.CorrelationId,
                        ["causationId"] = evt.CausationId,
                        ["data"] = evt.PayloadJson is not null ? JsonSerializer.Deserialize<JsonElement>(evt.PayloadJson) : null
                    };
                    var payload = JsonSerializer.Serialize(payloadDict);
                    writer.WriteLine($"event: {sseEventType}");
                    writer.WriteLine($"data: {payload}");
                    writer.WriteLine();
                    writer.Flush();
                }
                catch { }
            }

            eventBus.OnUpdate += OnUpdated;

            try
            {
                await writer.WriteLineAsync(": connected");
                await writer.FlushAsync();

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(30000, ct);
                    await writer.WriteLineAsync(": keepalive");
                    await writer.FlushAsync();
                }
            }
            catch (OperationCanceledException) { }
            finally { eventBus.OnUpdate -= OnUpdated; }

            return Results.Empty;
        });

        app.MapGet("/api/v1/events/all", async (
            EnvironmentEventBus eventBus,
            HttpContext context,
            CancellationToken ct) =>
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            using var writer = new StreamWriter(context.Response.Body) { AutoFlush = false };

            void OnUpdated(object? sender, EnvironmentUpdatedEvent evt)
            {
                try
                {
                    var sseEventType = evt.EventType.StartsWith("need.") ? "need" :
                        evt.EventType.StartsWith("command.") ? "command" :
                        evt.EventType.StartsWith("environment.") ? "environment" : "event";
                    var payloadDict = new Dictionary<string, object?>
                    {
                        ["type"] = evt.EventType,
                        ["roomId"] = evt.RoomId.ToString(),
                        ["timestamp"] = evt.Timestamp,
                        ["correlationId"] = evt.CorrelationId,
                        ["causationId"] = evt.CausationId,
                        ["data"] = evt.PayloadJson is not null ? JsonSerializer.Deserialize<JsonElement>(evt.PayloadJson) : null
                    };
                    var payload = JsonSerializer.Serialize(payloadDict);
                    writer.WriteLine($"event: {sseEventType}");
                    writer.WriteLine($"data: {payload}");
                    writer.WriteLine();
                    writer.Flush();
                }
                catch { }
            }

            eventBus.OnUpdate += OnUpdated;

            try
            {
                await writer.WriteLineAsync(": connected");
                await writer.FlushAsync();
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(30000, ct);
                    await writer.WriteLineAsync(": keepalive");
                    await writer.FlushAsync();
                }
            }
            catch (OperationCanceledException) { }
            finally { eventBus.OnUpdate -= OnUpdated; }

            return Results.Empty;
        });
    }
}