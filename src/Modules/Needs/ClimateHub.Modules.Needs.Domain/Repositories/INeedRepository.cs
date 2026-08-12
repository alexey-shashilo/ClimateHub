using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Domain.Repositories;

public interface INeedRepository
{
    Task<Need?> GetByIdAsync(NeedId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<NeedDto>> GetByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task<IReadOnlyCollection<NeedDto>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<NeedDto>> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task<Need?> GetActiveByTypeAsync(RoomId roomId, NeedType type, CancellationToken ct = default);
    Task<IReadOnlyCollection<NeedDto>> GetNeedsPendingReconciliationAsync(CancellationToken ct = default);
    Task AddAsync(Need need, CancellationToken ct = default);
    Task UpdateAsync(Need need, CancellationToken ct = default);
    Task AddEvaluationAsync(NeedEvaluation evaluation, CancellationToken ct = default);
    Task<IReadOnlyCollection<NeedEvaluation>> GetEvaluationsAsync(NeedId needId, int limit = 50, CancellationToken ct = default);
}
