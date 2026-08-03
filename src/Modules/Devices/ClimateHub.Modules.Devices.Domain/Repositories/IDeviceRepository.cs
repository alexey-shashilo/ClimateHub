using ClimateHub.Modules.Devices.Domain.Aggregates;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Domain.Repositories;

public interface IDeviceRepository : IRepository<Aggregates.Device, DeviceId>
{
    Task<Aggregates.Device?> GetByHardwareIdAsync(string hardwareId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Aggregates.Device>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Aggregates.Device>> GetByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Aggregates.DeviceAssignment>> GetAssignmentsAsync(DeviceId deviceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(DeviceId id, CancellationToken cancellationToken = default);
    Task<bool> HardwareIdExistsAsync(string hardwareId, CancellationToken cancellationToken = default);
    Task UpdateAndSaveAsync(Domain.Aggregates.Device entity, CancellationToken cancellationToken = default);
    Task RemoveAndSaveAsync(Domain.Aggregates.Device entity, CancellationToken cancellationToken = default);
}