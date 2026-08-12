using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Building.Infrastructure.Repositories;

public class FloorRepository(BuildingDbContext context) : IFloorRepository
{
    public async Task<Domain.Aggregates.Floor?> GetByIdAsync(FloorId id, CancellationToken cancellationToken = default)
    {
        return await context.Floors
            .Include(f => f.Rooms)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Floor?> GetWithRoomsAsync(FloorId id, CancellationToken cancellationToken = default)
    {
        return await context.Floors
            .Include(f => f.Rooms)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(FloorId id, CancellationToken cancellationToken = default)
    {
        return await context.Floors.AnyAsync(f => f.Id == id, cancellationToken);
    }

    public async Task AddAsync(Domain.Aggregates.Floor entity, CancellationToken cancellationToken = default)
    {
        await context.Floors.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Update(Domain.Aggregates.Floor entity)
    {
        context.Floors.Update(entity);
    }

    public async Task UpdateAndSaveAsync(Domain.Aggregates.Floor entity, CancellationToken cancellationToken = default)
    {
        context.Floors.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Domain.Aggregates.Floor entity)
    {
        context.Floors.Remove(entity);
    }

    public async Task RemoveAndSaveAsync(Domain.Aggregates.Floor entity, CancellationToken cancellationToken = default)
    {
        context.Floors.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
