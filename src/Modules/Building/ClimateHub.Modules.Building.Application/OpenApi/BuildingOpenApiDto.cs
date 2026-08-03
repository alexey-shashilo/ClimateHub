namespace ClimateHub.Modules.Building.Application.OpenApi;

public record BuildingSummaryDto(
    string Id,
    string Name,
    string Status,
    int FloorsCount,
    int RoomsCount,
    int RoomsWithWarnings,
    int DevicesOnline,
    int DevicesOffline,
    DateTimeOffset UpdatedAt);

public record FloorSummaryDto(
    string Id,
    string BuildingId,
    string Name,
    int Number,
    IReadOnlyCollection<RoomSummaryDto> Rooms);

public record RoomSummaryDto(
    string Id,
    string FloorId,
    string Name,
    string? Type,
    string Status,
    RoomEnvironmentSummaryDto Environment,
    int ActiveWarningsCount,
    int DevicesOnline,
    int DevicesTotal,
    DateTimeOffset UpdatedAt);

public record RoomEnvironmentSummaryDto(
    double? TemperatureC,
    double? RelativeHumidityPct,
    double? Co2Ppm,
    string? Quality);