using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Domain.Repositories;

public interface IClimatePlanRepository
{
    Task<ClimatePlan?> GetByIdAsync(ClimatePlanId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimatePlan>> GetByGoalAsync(ClimateGoalId goalId, CancellationToken ct = default);
    Task<ClimatePlan?> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimatePlan>> GetByStatusAsync(ClimatePlanStatus status, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimatePlan>> GetDueForEffectAsync(TimeSpan maxEffectWait, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimatePlan>> GetActivePlansAsync(CancellationToken ct = default);
    Task AddAsync(ClimatePlan plan, CancellationToken ct = default);
    Task UpdateAsync(ClimatePlan plan, CancellationToken ct = default);
}