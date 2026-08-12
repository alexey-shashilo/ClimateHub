using ClimateHub.Modules.Needs.Domain;
using ClimateHub.Modules.Needs.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class NeedsDbContext(DbContextOptions<NeedsDbContext> options) : DbContext(options)
{
    public DbSet<Need> Needs => Set<Need>();
    public DbSet<NeedEvaluation> NeedEvaluations => Set<NeedEvaluation>();
    public DbSet<RoomParameterEvaluationState> RoomParameterEvaluationStates => Set<RoomParameterEvaluationState>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("needs");
        mb.Entity<Need>(e =>
        {
            e.ToTable("needs");
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasConversion(v => v.Value, v => NeedId.From(v)).ValueGeneratedNever();
            e.Property(n => n.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(n => n.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
            e.Property(n => n.Severity).HasConversion<string>().HasMaxLength(20);
            e.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(n => n.Mode).HasConversion<string>().HasMaxLength(20);
            e.Property(n => n.SourceParameterCode).HasMaxLength(50);
            e.Property(n => n.PlanningFailureCode).HasMaxLength(100);
            e.Property(n => n.GeneratedByPolicyVersion).HasMaxLength(50);
            e.Property(n => n.ActiveCommandId).HasConversion(v => v!.Value.Value, v => new CommandId(v));
            e.Property(n => n.LastCommandId).HasConversion(v => v!.Value.Value, v => new CommandId(v));
            e.Property(n => n.SelectedDeviceId).HasConversion(v => v!.Value.Value, v => DeviceId.From(v));
            e.Property(n => n.SelectedCapabilityCode).HasMaxLength(100);
            e.Property(n => n.Version).IsRequired().IsConcurrencyToken();
            e.Ignore(n => n.DomainEvents);

            e.HasIndex(n => new { n.RoomId, n.Type })
                .HasFilter("Status IN ('Detected', 'Planning', 'Planned', 'Executing', 'WaitingForEffect', 'Blocked')")
                .IsUnique();

            e.HasIndex(n => new { n.Status, n.CooldownUntil });
            e.HasIndex(n => new { n.Status, n.EffectEvaluationDueAt });
            e.HasIndex(n => new { n.Status, n.LastEvaluationAt });
            e.HasIndex(n => n.ActiveCommandId);
            e.HasIndex(n => new { n.RoomId, n.Status });
            e.HasIndex(n => new { n.BuildingId, n.Status });
        });

        mb.Entity<RoomParameterEvaluationState>(e =>
        {
            e.ToTable("room_parameter_evaluation_states");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(x => x.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(x => x.ParameterCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.PolicyVersion).HasMaxLength(50);
            e.Property(x => x.ViolationDirection).HasMaxLength(10);
            e.Property(x => x.LastQuality).HasMaxLength(20);

            e.HasIndex(x => new { x.RoomId, x.ParameterCode }).IsUnique();
            e.HasIndex(x => x.ViolationSince);
            e.HasIndex(x => x.LastEvaluationAt);
        });

        mb.Entity<NeedEvaluation>(e =>
        {
            e.ToTable("need_evaluations");
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Id).ValueGeneratedOnAdd();
            e.Property(ev => ev.NeedId).HasConversion(v => v.Value, v => NeedId.From(v));
            e.Property(ev => ev.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(ev => ev.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(ev => ev.Trigger).HasConversion<string>().HasMaxLength(50);
            e.Property(ev => ev.CommandId).HasConversion(v => v!.Value.Value, v => new CommandId(v));
            e.Property(ev => ev.Outcome).HasMaxLength(50);
            e.Property(ev => ev.FailureCode).HasMaxLength(100);
            e.Property(ev => ev.CorrelationId).HasMaxLength(100);
            e.Property(ev => ev.CausationId).HasMaxLength(100);

            e.HasIndex(ev => ev.NeedId);
            e.HasIndex(ev => ev.EvaluatedAt);
        });
    }
}

public class RoomParameterEvaluationStateRepository(NeedsDbContext ctx) : IRoomParameterEvaluationStateRepository
{
    public async Task<RoomParameterEvaluationState?> GetByRoomAndParameterAsync(RoomId roomId, string parameterCode, CancellationToken ct = default)
        => await ctx.RoomParameterEvaluationStates.FirstOrDefaultAsync(
            x => x.RoomId == roomId && x.ParameterCode == parameterCode, ct);

    public async Task AddAsync(RoomParameterEvaluationState state, CancellationToken ct = default)
    {
        await ctx.RoomParameterEvaluationStates.AddAsync(state, ct);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RoomParameterEvaluationState state, CancellationToken ct = default)
    {
        ctx.RoomParameterEvaluationStates.Update(state);
        await ctx.SaveChangesAsync(ct);
    }
}

public class NeedsRepository(NeedsDbContext ctx) : INeedRepository
{
    public async Task<Need?> GetByIdAsync(NeedId id, CancellationToken ct = default) =>
        await ctx.Needs.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyCollection<NeedDto>> GetByRoomAsync(RoomId roomId, CancellationToken ct = default)
        => await ctx.Needs.Where(n => n.RoomId == roomId).OrderByDescending(n => n.CreatedAt).Select(n => ToDto(n)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<NeedDto>> GetActiveAsync(CancellationToken ct = default)
        => await ctx.Needs.Where(n => n.Status == NeedStatus.Detected || n.Status == NeedStatus.Planning || n.Status == NeedStatus.Planned || n.Status == NeedStatus.Executing || n.Status == NeedStatus.WaitingForEffect || n.Status == NeedStatus.Blocked)
            .OrderByDescending(n => n.Severity).Select(n => ToDto(n)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<NeedDto>> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default)
        => await ctx.Needs.Where(n => n.RoomId == roomId && (n.Status == NeedStatus.Detected || n.Status == NeedStatus.Planning || n.Status == NeedStatus.Planned || n.Status == NeedStatus.Executing || n.Status == NeedStatus.WaitingForEffect || n.Status == NeedStatus.Blocked))
            .OrderByDescending(n => n.Severity).Select(n => ToDto(n)).ToListAsync(ct);

    public async Task<Need?> GetActiveByTypeAsync(RoomId roomId, NeedType type, CancellationToken ct = default)
        => await ctx.Needs.FirstOrDefaultAsync(n =>
            n.RoomId == roomId && n.Type == type &&
            (n.Status == NeedStatus.Detected || n.Status == NeedStatus.Planning || n.Status == NeedStatus.Planned ||
             n.Status == NeedStatus.Executing || n.Status == NeedStatus.WaitingForEffect || n.Status == NeedStatus.Blocked), ct);

    public async Task<IReadOnlyCollection<NeedDto>> GetNeedsPendingReconciliationAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await ctx.Needs.Where(n =>
            n.Status == NeedStatus.Detected || n.Status == NeedStatus.Planning || n.Status == NeedStatus.Planned ||
            n.Status == NeedStatus.Executing || n.Status == NeedStatus.WaitingForEffect || n.Status == NeedStatus.Blocked)
            .Where(n => n.CooldownUntil == null || n.CooldownUntil <= now)
            .Where(n => n.EffectEvaluationDueAt == null || n.EffectEvaluationDueAt <= now)
            .Where(n => n.LastEvaluationAt == null || n.LastEvaluationAt <= now.AddMinutes(-2))
            .OrderByDescending(n => n.Severity).Select(n => ToDto(n)).ToListAsync(ct);
    }

    public async Task AddAsync(Need need, CancellationToken ct = default)
    {
        await ctx.Needs.AddAsync(need, ct);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Need need, CancellationToken ct = default)
    {
        ctx.Needs.Update(need);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task AddEvaluationAsync(NeedEvaluation evaluation, CancellationToken ct = default)
    {
        await ctx.NeedEvaluations.AddAsync(evaluation, ct);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<NeedEvaluation>> GetEvaluationsAsync(NeedId needId, int limit = 50, CancellationToken ct = default)
        => await ctx.NeedEvaluations.Where(ev => ev.NeedId == needId)
            .OrderByDescending(ev => ev.EvaluatedAt).Take(limit).ToListAsync(ct);

    private static NeedDto ToDto(Need n) => new(
        n.Id, n.Type.ToString(), n.Severity.ToString(), n.Status.ToString(),
        n.DesiredMin, n.DesiredMax, n.DesiredPreferred, n.CurrentValue, n.Deviation,
        n.SourceParameterCode, n.RoomId, n.SelectedCapabilityCode, n.ActiveCommandId,
        n.SelectedDeviceId, n.SelectedEngineeringSystemId, n.SelectedEngineeringCapabilityCode,
        n.ActiveCommandPlanId, n.PlanningFailureCode, n.CreatedAt, n.UpdatedAt, n.ResolvedAt,
        n.LastEvaluationAt, n.Version, n.Mode, n.ViolationSince, n.StableSince, n.CooldownUntil,
        n.EffectEvaluationDueAt, n.LastCommandCreatedAt, n.LastMeaningfulImprovementAt,
        n.PlanningAttemptCount, n.CommandAttemptCount, n.LastCommandId);
}
