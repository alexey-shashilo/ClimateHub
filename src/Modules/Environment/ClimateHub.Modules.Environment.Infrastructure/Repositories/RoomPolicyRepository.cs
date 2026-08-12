using ClimateHub.Modules.Environment.Domain;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public class RoomPolicyRepository(EnvironmentDbContext context) : IRoomPolicyRepository
{
    public async Task<RoomPolicy?> GetByRoomAsync(RoomId roomId, CancellationToken ct = default)
        => await context.Set<RoomPolicy>().FirstOrDefaultAsync(p => p.Id == roomId, ct);

    public async Task UpsertAsync(RoomPolicy policy, CancellationToken ct = default)
    {
        var existing = await context.Set<RoomPolicy>().FirstOrDefaultAsync(p => p.Id == policy.Id, ct);
        if (existing is null)
            await context.Set<RoomPolicy>().AddAsync(policy, ct);
        else
        {
            existing.Update(
                policy.TemperatureMin, policy.TemperatureMax, policy.TemperaturePreferred, policy.TemperatureMode,
                policy.HumidityMin, policy.HumidityMax, policy.HumidityPreferred, policy.HumidityMode,
                policy.Co2Min, policy.Co2Max, policy.Co2Preferred, policy.Co2Mode,
                policy.IlluminanceMin, policy.IlluminanceMax, policy.IlluminancePreferred, policy.IlluminanceMode);
        }
        await context.SaveChangesAsync(ct);
    }
}
