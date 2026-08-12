using ClimateHub.Modules.Environment.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public interface IRoomPolicyRepository
{
    Task<RoomPolicy?> GetByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task UpsertAsync(RoomPolicy policy, CancellationToken ct = default);
}
