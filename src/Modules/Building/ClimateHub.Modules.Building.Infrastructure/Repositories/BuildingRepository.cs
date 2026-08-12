using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Building.Infrastructure.Repositories;

public class BuildingRepository(BuildingDbContext context) : IBuildingRepository
{
    public async Task<Domain.Aggregates.Building?> GetByIdAsync(BuildingId id, CancellationToken cancellationToken = default)
    {
        return await context.Buildings
            .Include(b => b.Floors)
            .ThenInclude(f => f.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Building?> GetWithFloorsAsync(BuildingId id, CancellationToken cancellationToken = default)
    {
        return await context.Buildings
            .Include(b => b.Floors)
            .ThenInclude(f => f.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(BuildingId id, CancellationToken cancellationToken = default)
    {
        return await context.Buildings.AnyAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Room?> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Aggregates.Building>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Buildings
            .Include(b => b.Floors)
            .ThenInclude(f => f.Rooms)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Domain.Aggregates.Building entity, CancellationToken cancellationToken = default)
    {
        await context.Buildings.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Update(Domain.Aggregates.Building entity)
    {
        context.Buildings.Update(entity);
    }

    public async Task UpdateAndSaveAsync(Domain.Aggregates.Building entity, CancellationToken cancellationToken = default)
    {
        context.Buildings.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Domain.Aggregates.Building entity)
    {
        context.Buildings.Remove(entity);
    }

    public async Task RemoveAndSaveAsync(Domain.Aggregates.Building entity, CancellationToken cancellationToken = default)
    {
        context.Buildings.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
