using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Building.Infrastructure.Repositories;

public class RoomRepository(BuildingDbContext context) : IRoomRepository
{
    public async Task<Domain.Aggregates.Room?> GetByIdAsync(RoomId id, CancellationToken cancellationToken = default)
    {
        return await context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(RoomId id, CancellationToken cancellationToken = default)
    {
        return await context.Rooms.AnyAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(Domain.Aggregates.Room entity, CancellationToken cancellationToken = default)
    {
        await context.Rooms.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Update(Domain.Aggregates.Room entity)
    {
        context.Rooms.Update(entity);
    }

    public async Task UpdateAndSaveAsync(Domain.Aggregates.Room entity, CancellationToken cancellationToken = default)
    {
        context.Rooms.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Domain.Aggregates.Room entity)
    {
        context.Rooms.Remove(entity);
    }

    public async Task RemoveAndSaveAsync(Domain.Aggregates.Room entity, CancellationToken cancellationToken = default)
    {
        context.Rooms.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
