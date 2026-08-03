using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Contracts;

public record EnvironmentParameterDto(
    double Value,
    string Unit,
    DateTimeOffset MeasuredAt,
    DateTimeOffset ReceivedAt,
    string Quality,
    DeviceId SourceDeviceId,
    EnvironmentTargetDto? Target);

public record EnvironmentTargetDto(double? Minimum, double? Maximum, double? Preferred);

public record RoomEnvironmentResponseDto(
    RoomId RoomId,
    string RoomName,
    string Status,
    EnvironmentParametersDto Parameters,
    DateTimeOffset UpdatedAt);

public record EnvironmentParametersDto(
    EnvironmentParameterDto? Temperature,
    EnvironmentParameterDto? RelativeHumidity,
    EnvironmentParameterDto? Co2);