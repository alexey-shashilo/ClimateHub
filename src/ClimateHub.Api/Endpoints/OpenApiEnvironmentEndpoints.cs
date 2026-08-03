using ClimateHub.Modules.Building.Application.Queries;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class OpenApiEnvironmentEndpoints
{
    public static void MapOpenApiEnvironmentEndpoints(this WebApplication app)
    {
        var eg = app.MapGroup("/api/v1/rooms").WithTags("Environment");

        eg.MapGet("/{roomId}/environment", async (
            string roomId,
            IRoomEnvironmentStateRepository stateRepo,
            BuildingOpenApiService buildingService,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.NotFound();
            var rid = RoomId.From(guid);
            var parameters = await stateRepo.GetParametersAsync(rid, ct);
            if (parameters.Count == 0)
                return Results.NotFound();

            var roomName = await buildingService.GetRoomNameAsync(rid, ct);

            var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
            var humParam = parameters.FirstOrDefault(p => p.Parameter == "humidity");
            var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");
            var illParam = parameters.FirstOrDefault(p => p.Parameter == "illuminance");

            object? temp = tempParam is not null ? ToParamObj(tempParam, "celsius") : null;
            object? hum = humParam is not null ? ToParamObj(humParam, "percent") : null;
            object? co2 = co2Param is not null ? ToParamObj(co2Param, "ppm") : null;
            object? ill = illParam is not null ? ToParamObj(illParam, "lux") : null;

            return Results.Ok(new
            {
                roomId = rid.ToString(),
                roomName,
                status = ClassifyStatus(tempParam, humParam, co2Param),
                parameters = new { temperature = temp, relativeHumidity = hum, co2, illuminance = ill },
                updatedAt = DateTimeOffset.UtcNow
            });
        });

        eg.MapGet("/{roomId}/environment/history", async (
            string roomId,
            IEnvironmentModule envModule,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string? parameter,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.NotFound();
            var rid = RoomId.From(guid);

            var fromDate = from ?? DateTimeOffset.UtcNow.AddHours(-24);
            var toDate = to ?? DateTimeOffset.UtcNow;

            if (fromDate >= toDate)
                return Results.BadRequest(new { code = "HISTORY_RANGE_TOO_LARGE", detail = "from must be before to" });

            if ((toDate - fromDate).TotalDays > 30)
                return Results.BadRequest(new { code = "HISTORY_RANGE_TOO_LARGE", detail = "History range cannot exceed 30 days" });

            if (parameter is not null && parameter is not ("temperature" or "humidity" or "co2" or "illuminance"))
                return Results.BadRequest(new { code = "INVALID_PARAMETER" });

            var measurements = await envModule.GetHistoryAsync(rid, fromDate, toDate, parameter, ct);

            var paramCode = parameter ?? "temperature";
            var unit = paramCode switch
            {
                "temperature" => "celsius",
                "humidity" => "percent",
                "co2" => "ppm",
                "illuminance" => "lux",
                _ => ""
            };

            var points = measurements
                .Select(m => new { timestamp = m.Timestamp, value = m.Value, quality = m.Quality })
                .ToList();

            return Results.Ok(new
            {
                roomId = rid.ToString(),
                parameter = paramCode,
                unit,
                from = fromDate,
                to = toDate,
                aggregation = "raw",
                points
            });
        });
    }

    private static string ClassifyStatus(RoomParameterEntity? temp, RoomParameterEntity? hum, RoomParameterEntity? co2)
    {
        if (temp is null && hum is null && co2 is null) return "unknown";
        if (co2?.Value > 1400) return "critical";
        if (co2?.Value > 1000) return "warning";
        if (hum?.Value is < 20 or > 70) return "warning";
        if (temp?.Value is < 16 or > 32) return "warning";
        return "normal";
    }

    private static object ToParamObj(RoomParameterEntity e, string unit)
    {
        var target = e.Parameter switch
        {
            "temperature" => new { minimum = 20.0, maximum = 26.0, preferred = 23.0 },
            "humidity" => new { minimum = 30.0, maximum = 60.0, preferred = 45.0 },
            "co2" => new { minimum = 0.0, maximum = 1000.0, preferred = 600.0 },
            "illuminance" => new { minimum = 300.0, maximum = 750.0, preferred = 500.0 },
            _ => null
        };

        return new
        {
            value = e.Value ?? 0,
            unit,
            measuredAt = e.MeasuredAt ?? DateTimeOffset.UtcNow,
            receivedAt = e.ReceivedAt,
            quality = e.Quality,
            sourceDeviceId = e.SourceDeviceId?.ToString() ?? Guid.Empty.ToString(),
            target
        };
    }
}