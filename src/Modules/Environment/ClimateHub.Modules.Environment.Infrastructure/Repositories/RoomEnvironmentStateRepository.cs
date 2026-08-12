using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public class RoomEnvironmentStateRepository(EnvironmentDbContext context) : IRoomEnvironmentStateRepository
{
    public async Task<IReadOnlyCollection<RoomParameterEntity>> GetParametersAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await context.RoomParameters
            .Where(p => p.RoomId == roomId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertParameterAsync(RoomParameterEntity entity, CancellationToken cancellationToken = default)
    {
        var existing = await context.RoomParameters
            .FirstOrDefaultAsync(p => p.RoomId == entity.RoomId && p.Parameter == entity.Parameter, cancellationToken);

        if (existing is null)
        {
            await context.RoomParameters.AddAsync(entity, cancellationToken);
        }
        else
        {
            existing.Value = entity.Value;
            existing.MeasuredAt = entity.MeasuredAt;
            existing.ReceivedAt = entity.ReceivedAt;
            existing.Quality = entity.Quality;
            existing.SourceDeviceId = entity.SourceDeviceId;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
