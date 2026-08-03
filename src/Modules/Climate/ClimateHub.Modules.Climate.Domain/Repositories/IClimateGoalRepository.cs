using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Domain.Repositories;

public interface IClimateGoalRepository
{
    Task<ClimateGoal?> GetByIdAsync(ClimateGoalId id, CancellationToken ct = default);
    Task<ClimateGoal?> GetByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimateGoal>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimateGoal>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(ClimateGoal goal, CancellationToken ct = default);
    Task UpdateAsync(ClimateGoal goal, CancellationToken ct = default);
    Task<bool> DeleteAsync(ClimateGoalId id, CancellationToken ct = default);
}