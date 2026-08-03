using ClimateHub.Modules.Devices.Domain.Aggregates;
using ClimateHub.Modules.Devices.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Devices.Infrastructure.Repositories;

public class DeviceRepository(DevicesDbContext context) : IDeviceRepository
{
    public async Task<Domain.Aggregates.Device?> GetByIdAsync(DeviceId id, CancellationToken cancellationToken = default)
    {
        return await context.Devices
            .Include(d => d.Capabilities)
            .Include(d => d.AssignmentHistory)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Device?> GetByHardwareIdAsync(string hardwareId, CancellationToken cancellationToken = default)
    {
        return await context.Devices
            .Include(d => d.Capabilities)
            .FirstOrDefaultAsync(d => d.HardwareId == hardwareId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Aggregates.Device>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Devices
            .Include(d => d.Capabilities)
            .Include(d => d.AssignmentHistory)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Aggregates.Device>> GetByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var activeAssignmentIds = await context.Assignments
            .Where(a => a.RoomId == roomId && a.Status == AssignmentStatus.Active)
            .Select(a => a.DeviceId)
            .ToListAsync(cancellationToken);

        return await context.Devices
            .Include(d => d.Capabilities)
            .Include(d => d.AssignmentHistory)
            .Where(d => activeAssignmentIds.Contains(d.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Aggregates.DeviceAssignment>> GetAssignmentsAsync(DeviceId deviceId, CancellationToken cancellationToken = default)
    {
        return await context.Assignments
            .Where(a => a.DeviceId == deviceId)
            .OrderByDescending(a => a.AssignedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(DeviceId id, CancellationToken cancellationToken = default)
    {
        return await context.Devices.AnyAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<bool> HardwareIdExistsAsync(string hardwareId, CancellationToken cancellationToken = default)
    {
        return await context.Devices.AnyAsync(d => d.HardwareId == hardwareId, cancellationToken);
    }

    public async Task AddAsync(Domain.Aggregates.Device entity, CancellationToken cancellationToken = default)
    {
        await context.Devices.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Update(Domain.Aggregates.Device entity)
    {
        context.Devices.Update(entity);
    }

    public async Task UpdateAndSaveAsync(Domain.Aggregates.Device entity, CancellationToken cancellationToken = default)
    {
        context.Devices.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Domain.Aggregates.Device entity)
    {
        context.Devices.Remove(entity);
    }

    public async Task RemoveAndSaveAsync(Domain.Aggregates.Device entity, CancellationToken cancellationToken = default)
    {
        context.Devices.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}