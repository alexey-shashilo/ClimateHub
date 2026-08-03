using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Climate.Infrastructure.Repositories;

public class ClimateGoalRepository : IClimateGoalRepository
{
    private readonly ClimateDbContext _db;

    public ClimateGoalRepository(ClimateDbContext db) { _db = db; }

    public async Task<ClimateGoal?> GetByIdAsync(ClimateGoalId id, CancellationToken ct = default) =>
        await _db.ClimateGoals.FindAsync([id], ct);

    public async Task<ClimateGoal?> GetByRoomAsync(RoomId roomId, CancellationToken ct = default) =>
        await _db.ClimateGoals
            .Where(g => g.RoomId == roomId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyCollection<ClimateGoal>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default) =>
        await _db.ClimateGoals.Where(g => g.BuildingId == buildingId).ToListAsync(ct);

    public async Task<IReadOnlyCollection<ClimateGoal>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.ClimateGoals
            .Where(g => g.Status == GoalStatus.Active || g.Status == GoalStatus.Planning
                || g.Status == GoalStatus.Executing || g.Status == GoalStatus.WaitingForEffect
                || g.Status == GoalStatus.Blocked)
            .ToListAsync(ct);

    public async Task AddAsync(ClimateGoal goal, CancellationToken ct = default) { await _db.ClimateGoals.AddAsync(goal, ct); await _db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(ClimateGoal goal, CancellationToken ct = default) { _db.ClimateGoals.Update(goal); await _db.SaveChangesAsync(ct); }
    public async Task<bool> DeleteAsync(ClimateGoalId id, CancellationToken ct = default) { var goal = await GetByIdAsync(id, ct); if (goal is null) return false; _db.ClimateGoals.Remove(goal); await _db.SaveChangesAsync(ct); return true; }
}