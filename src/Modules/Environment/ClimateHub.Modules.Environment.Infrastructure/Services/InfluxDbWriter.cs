using ClimateHub.SharedKernel.Configuration;
using ClimateHub.SharedKernel.Primitives;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClimateHub.Modules.Environment.Infrastructure.Services;

public class InfluxDbWriter : IInfluxDbWriter, IInfluxDbReader, IDisposable
{
    private readonly InfluxDBClient _client;
    private readonly IOptions<InfluxDbOptions> _options;
    private readonly ILogger<InfluxDbWriter> _logger;

    public InfluxDbWriter(
        IOptions<InfluxDbOptions> options,
        ILogger<InfluxDbWriter> logger)
    {
        _options = options;
        _logger = logger;
        var token = string.IsNullOrWhiteSpace(options.Value.Token) ? "climate-hub-dev-token" : options.Value.Token;
        _client = new InfluxDBClient(options.Value.Url, token);
    }

    public Task WriteMeasurementAsync(
        RoomId roomId,
        DeviceId deviceId,
        DateTimeOffset measuredAt,
        double? temperatureC,
        double? relativeHumidityPct,
        double? co2Ppm,
        string quality,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var point = PointData.Measurement("environment")
                .Tag("room_id", roomId.ToString())
                .Tag("device_id", deviceId.ToString())
                .Tag("quality", quality)
                .Timestamp(measuredAt.UtcDateTime, WritePrecision.Ms);

            if (temperatureC.HasValue)
                point = point.Field("temperature_c", temperatureC.Value);
            if (relativeHumidityPct.HasValue)
                point = point.Field("relative_humidity_pct", relativeHumidityPct.Value);
            if (co2Ppm.HasValue)
                point = point.Field("co2_ppm", co2Ppm.Value);

            using var writeApi = _client.GetWriteApi();
            writeApi.WritePoint(point, _options.Value.Bucket, _options.Value.Organization);

            _logger.LogDebug(
                "Written to InfluxDB: room={RoomId} device={DeviceId}", roomId, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InfluxDB write error for room {RoomId} device {DeviceId}", roomId, deviceId);
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<InfluxMeasurement>> ReadHistoryAsync(
        RoomId roomId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $@"
                from(bucket: ""{_options.Value.Bucket}"")
                    |> range(start: {from:O}, stop: {to:O})
                    |> filter(fn: (r) => r._measurement == ""environment"" and r.room_id == ""{roomId}"")
                    |> pivot(rowKey:[""_time""], columnKey:[""_field""], valueColumn:""_value"")";

            var tables = await _client.GetQueryApi().QueryAsync(query, _options.Value.Organization, cancellationToken);

            var results = new List<InfluxMeasurement>();
            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var measuredAt = record.GetTime() is { } instant
                        ? DateTimeOffset.FromUnixTimeMilliseconds(instant.ToUnixTimeMilliseconds())
                        : DateTimeOffset.UtcNow;

                    record.Values.TryGetValue("temperature_c", out var tempObj);
                    record.Values.TryGetValue("relative_humidity_pct", out var humObj);
                    record.Values.TryGetValue("co2_ppm", out var co2Obj);
                    record.Values.TryGetValue("quality", out var qualityObj);

                    results.Add(new InfluxMeasurement(
                        measuredAt,
                        tempObj as double?,
                        humObj as double?,
                        co2Obj as double?,
                        qualityObj as string ?? "valid"));
                }
            }

            return results.OrderByDescending(r => r.MeasuredAt).Take(1000).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InfluxDB read error for room {RoomId}", roomId);
            return [];
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}