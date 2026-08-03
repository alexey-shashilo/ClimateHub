using ClimateHub.Modules.Building.Application.OpenApi;
using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Application.Queries;

public class BuildingOpenApiService
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly IDevicesModule _devicesModule;

    public BuildingOpenApiService(IBuildingRepository buildingRepository, IDevicesModule devicesModule)
    {
        _buildingRepository = buildingRepository;
        _devicesModule = devicesModule;
    }

    public async Task<IReadOnlyCollection<BuildingSummaryDto>> ListBuildingsAsync(CancellationToken ct = default)
    {
        var buildings = await _buildingRepository.GetAllAsync(ct);
        return buildings.Select(b =>
        {
            var allRooms = b.Floors.SelectMany(f => f.Rooms).ToList();
            return new BuildingSummaryDto(
                b.Id.ToString(), b.Name, "normal",
                b.Floors.Count, allRooms.Count, 0, 0, 0, DateTimeOffset.UtcNow);
        }).ToList();
    }

    public async Task<BuildingSummaryDto?> GetBuildingSummaryAsync(BuildingId buildingId, CancellationToken ct = default)
    {
        var building = await _buildingRepository.GetByIdAsync(buildingId, ct);
        if (building is null) return null;

        var allRooms = building.Floors.SelectMany(f => f.Rooms).ToList();

        return new BuildingSummaryDto(
            building.Id.ToString(), building.Name, "normal",
            building.Floors.Count, allRooms.Count, 0,
            0, 0, DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyCollection<FloorSummaryDto>> GetFloorsAsync(BuildingId buildingId, CancellationToken ct = default)
    {
        var building = await _buildingRepository.GetByIdAsync(buildingId, ct);
        if (building is null) return [];

        var result = new List<FloorSummaryDto>();
        foreach (var f in building.Floors)
        {
            var rooms = new List<RoomSummaryDto>();
            foreach (var r in f.Rooms)
            {
                var devices = await _devicesModule.GetRoomDevicesAsync(r.Id, ct);
                var online = devices.Count(d => d.Connectivity == "online" || d.Connectivity == "operational");
                rooms.Add(new RoomSummaryDto(
                    r.Id.ToString(), f.Id.ToString(), r.Name, null, "normal",
                    new RoomEnvironmentSummaryDto(null, null, null, null),
                    0, online, devices.Count, DateTimeOffset.UtcNow));
            }
            result.Add(new FloorSummaryDto(f.Id.ToString(), building.Id.ToString(), f.Name, f.Level, rooms));
        }
        return result;
    }

    public async Task<RoomSummaryDto?> GetRoomSummaryAsync(RoomId roomId, CancellationToken ct = default)
    {
        var room = await _buildingRepository.GetByRoomIdAsync(roomId, ct);
        if (room is null) return null;

        var devices = await _devicesModule.GetRoomDevicesAsync(roomId, ct);
        var online = devices.Count(d => d.Connectivity == "online" || d.Connectivity == "operational");

        return new RoomSummaryDto(
            room.Id.ToString(), room.FloorId.ToString(), room.Name, null, "normal",
            new RoomEnvironmentSummaryDto(null, null, null, null),
            0, online, devices.Count, DateTimeOffset.UtcNow);
    }

    public async Task<string> GetRoomNameAsync(RoomId roomId, CancellationToken ct = default)
    {
        var room = await _buildingRepository.GetByRoomIdAsync(roomId, ct);
        return room?.Name ?? $"Room {roomId}";
    }
}