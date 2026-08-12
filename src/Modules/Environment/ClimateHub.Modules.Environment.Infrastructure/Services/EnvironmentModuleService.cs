using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Environment.Domain;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Services;

public class EnvironmentModuleService(
    IRoomEnvironmentStateRepository stateRepository,
    IInfluxDbReader influxDbReader) : IEnvironmentModule
{
    public async Task<RoomEnvironmentStateDto?> GetCurrentStateAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var entities = await stateRepository.GetParametersAsync(roomId, cancellationToken);
        if (entities.Count == 0)
            return null;

        return MapToDto(roomId, entities);
    }

    public async Task<IReadOnlyCollection<EnvironmentMeasurementDto>> GetHistoryAsync(
        RoomId roomId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? parameter,
        CancellationToken cancellationToken = default)
    {
        var measurements = await influxDbReader.ReadHistoryAsync(roomId, from, to, cancellationToken);

        IEnumerable<EnvironmentMeasurementDto> filtered = parameter switch
        {
            "temperature" => measurements.Where(m => m.TemperatureC.HasValue)
                .Select(m => new EnvironmentMeasurementDto(m.MeasuredAt, m.TemperatureC!.Value, m.Quality)),
            "humidity" => measurements.Where(m => m.RelativeHumidityPct.HasValue)
                .Select(m => new EnvironmentMeasurementDto(m.MeasuredAt, m.RelativeHumidityPct!.Value, m.Quality)),
            "co2" => measurements.Where(m => m.Co2Ppm.HasValue)
                .Select(m => new EnvironmentMeasurementDto(m.MeasuredAt, m.Co2Ppm!.Value, m.Quality)),
            _ => ExpandAll(measurements)
        };

        return filtered.Take(1000).ToList();
    }

    private static IEnumerable<EnvironmentMeasurementDto> ExpandAll(IReadOnlyCollection<InfluxMeasurement> measurements)
    {
        foreach (var m in measurements)
        {
            if (m.TemperatureC.HasValue)
                yield return new EnvironmentMeasurementDto(m.MeasuredAt, m.TemperatureC!.Value, m.Quality);
            if (m.RelativeHumidityPct.HasValue)
                yield return new EnvironmentMeasurementDto(m.MeasuredAt, m.RelativeHumidityPct!.Value, m.Quality);
            if (m.Co2Ppm.HasValue)
                yield return new EnvironmentMeasurementDto(m.MeasuredAt, m.Co2Ppm!.Value, m.Quality);
        }
    }

    private static RoomEnvironmentStateDto MapToDto(RoomId roomId, IReadOnlyCollection<RoomParameterEntity> entities)
    {
        var temp = entities.FirstOrDefault(e => e.Parameter == "temperature");
        var hum = entities.FirstOrDefault(e => e.Parameter == "humidity");
        var co2 = entities.FirstOrDefault(e => e.Parameter == "co2");

        return new RoomEnvironmentStateDto(
            roomId,
            temp is not null ? MapParameter(temp, "celsius") : null,
            hum is not null ? MapParameter(hum, "percent") : null,
            co2 is not null ? MapParameter(co2, "ppm") : null);
    }

    private static ParameterStateDto MapParameter(RoomParameterEntity entity, string unit) => new(
        entity.Parameter,
        entity.Value,
        unit,
        entity.MeasuredAt,
        entity.ReceivedAt,
        entity.Quality ?? "unknown",
        entity.SourceDeviceId);
}
