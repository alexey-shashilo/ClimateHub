using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Contracts;

public interface IEnvironmentModule
{
    Task<RoomEnvironmentStateDto?> GetCurrentStateAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EnvironmentMeasurementDto>> GetHistoryAsync(
        RoomId roomId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? parameter,
        CancellationToken cancellationToken = default);
}

public interface IRoomPolicyReader
{
    Task<RoomPolicyDto?> GetByRoomAsync(RoomId roomId, CancellationToken ct = default);
}

public interface IRoomEnvironmentStateReader
{
    Task<IReadOnlyCollection<ParameterStateDto>> GetParametersAsync(RoomId roomId, CancellationToken ct = default);
}

public record ParameterStateDto(
    string Parameter,
    double? Value,
    string? Unit,
    DateTimeOffset? MeasuredAt,
    DateTimeOffset? ReceivedAt,
    string Quality,
    DeviceId? SourceDeviceId);

public record RoomPolicyDto(
    RoomId RoomId,
    double? TemperatureMin, double? TemperatureMax, double? TemperaturePreferred, string TemperatureMode,
    double? HumidityMin, double? HumidityMax, double? HumidityPreferred, string HumidityMode,
    double? Co2Min, double? Co2Max, double? Co2Preferred, string Co2Mode,
    double? IlluminanceMin, double? IlluminanceMax, double? IlluminancePreferred, string IlluminanceMode)
{
    public string GetControlMode(string parameter) => parameter switch
    {
        "temperature" => TemperatureMode,
        "humidity" => HumidityMode,
        "co2" => Co2Mode,
        "illuminance" => IlluminanceMode,
        _ => "disabled"
    };
}

public record RoomEnvironmentStateDto(
    RoomId RoomId,
    ParameterStateDto? Temperature,
    ParameterStateDto? Humidity,
    ParameterStateDto? Co2);

public record EnvironmentMeasurementDto(
    DateTimeOffset Timestamp,
    double Value,
    string Quality);
