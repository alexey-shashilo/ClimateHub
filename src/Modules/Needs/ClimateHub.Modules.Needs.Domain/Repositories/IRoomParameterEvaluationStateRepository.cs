using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Domain.Repositories;

public interface IRoomParameterEvaluationStateRepository
{
    Task<RoomParameterEvaluationState?> GetByRoomAndParameterAsync(RoomId roomId, string parameterCode, CancellationToken ct = default);
    Task AddAsync(RoomParameterEvaluationState state, CancellationToken ct = default);
    Task UpdateAsync(RoomParameterEvaluationState state, CancellationToken ct = default);
}
