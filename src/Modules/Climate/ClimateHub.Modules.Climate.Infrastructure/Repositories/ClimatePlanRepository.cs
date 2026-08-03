using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.Climate.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Climate.Infrastructure.Repositories;

public class ClimatePlanRepository : IClimatePlanRepository
{
    private readonly ClimateDbContext _db;

    public ClimatePlanRepository(ClimateDbContext db) { _db = db; }

    public async Task<ClimatePlan?> GetByIdAsync(ClimatePlanId id, CancellationToken ct = default) =>
        await _db.ClimatePlans
            .Include(p => p.SubPlans)
            .Include(p => p.Dependencies)
            .Include(p => p.ResolvedConflicts)
            .Include(p => p.ResourceReservations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyCollection<ClimatePlan>> GetByGoalAsync(ClimateGoalId goalId, CancellationToken ct = default) =>
        await _db.ClimatePlans.Where(p => p.GoalId == goalId).OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

    public async Task<ClimatePlan?> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default) =>
        await _db.ClimatePlans
            .Include(p => p.SubPlans)
            .Include(p => p.Dependencies)
            .Include(p => p.ResolvedConflicts)
            .Include(p => p.ResourceReservations)
            .Where(p => p.RoomId == roomId && (p.Status == ClimatePlanStatus.Planning
                || p.Status == ClimatePlanStatus.Planned || p.Status == ClimatePlanStatus.ReservingResources
                || p.Status == ClimatePlanStatus.Ready || p.Status == ClimatePlanStatus.Executing
                || p.Status == ClimatePlanStatus.WaitingForEffect))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyCollection<ClimatePlan>> GetByStatusAsync(ClimatePlanStatus status, CancellationToken ct = default) =>
        await _db.ClimatePlans.Where(p => p.Status == status).Include(p => p.SubPlans).ToListAsync(ct);

    public async Task<IReadOnlyCollection<ClimatePlan>> GetDueForEffectAsync(TimeSpan maxEffectWait, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(maxEffectWait);
        return await _db.ClimatePlans
            .Include(p => p.SubPlans)
            .Where(p => p.Status == ClimatePlanStatus.WaitingForEffect && p.WaitingForEffectAt != null && p.WaitingForEffectAt < cutoff)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<ClimatePlan>> GetActivePlansAsync(CancellationToken ct = default) =>
        await _db.ClimatePlans
            .Include(p => p.SubPlans)
            .Where(p => p.Status == ClimatePlanStatus.Planning || p.Status == ClimatePlanStatus.Planned
                || p.Status == ClimatePlanStatus.ReservingResources || p.Status == ClimatePlanStatus.Ready
                || p.Status == ClimatePlanStatus.Executing || p.Status == ClimatePlanStatus.WaitingForEffect)
            .ToListAsync(ct);

    public async Task AddAsync(ClimatePlan plan, CancellationToken ct = default) { await _db.ClimatePlans.AddAsync(plan, ct); await _db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(ClimatePlan plan, CancellationToken ct = default) { _db.ClimatePlans.Update(plan); await _db.SaveChangesAsync(ct); }
}