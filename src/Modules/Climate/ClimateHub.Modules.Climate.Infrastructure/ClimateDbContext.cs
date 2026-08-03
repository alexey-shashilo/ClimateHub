using ClimateHub.Modules.Climate.Domain;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.Climate.Domain.Resources;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Climate.Infrastructure;

public class ClimateDbContext(DbContextOptions<ClimateDbContext> options) : DbContext(options)
{
    public DbSet<ClimateGoal> ClimateGoals => Set<ClimateGoal>();
    public DbSet<ClimatePlan> ClimatePlans => Set<ClimatePlan>();
    public DbSet<EngineeringSubPlan> EngineeringSubPlans => Set<EngineeringSubPlan>();
    public DbSet<ClimatePlanDependency> ClimatePlanDependencies => Set<ClimatePlanDependency>();
    public DbSet<ClimateConflict> ClimateConflicts => Set<ClimateConflict>();
    public DbSet<ClimateResource> ClimateResources => Set<ClimateResource>();
    public DbSet<ClimateResourceReservation> ClimateResourceReservations => Set<ClimateResourceReservation>();
    public DbSet<ClimateEventInboxEntry> ClimateEventInbox => Set<ClimateEventInboxEntry>();
    public DbSet<ClimateEvaluationEntry> ClimateEvaluations => Set<ClimateEvaluationEntry>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("climate");

        mb.Entity<ClimateGoal>(e =>
        {
            e.ToTable("climate_goals");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasConversion(v => v.Value, v => ClimateGoalId.From(v)).ValueGeneratedNever();
            e.Property(g => g.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(g => g.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(g => g.ActiveProfile).HasConversion<string>().HasMaxLength(20);
            e.Property(g => g.ActiveClimatePlanId).HasConversion(v => v!.Value.Value, v => ClimatePlanId.From(v));
            e.Property(g => g.PolicyVersion).HasMaxLength(50);
            e.Property(g => g.EnvironmentStateVersion).HasMaxLength(50);
            e.Property(g => g.Version).IsRequired().IsConcurrencyToken();
            e.Ignore(g => g.DomainEvents);
            e.HasIndex(g => g.RoomId).IsUnique().HasFilter("Status IN ('Active','Planning','Executing','WaitingForEffect','Blocked')");
            e.HasIndex(g => new { g.BuildingId, g.Status });
        });

        mb.Entity<ClimatePlan>(e =>
        {
            e.ToTable("climate_plans");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasConversion(v => v.Value, v => ClimatePlanId.From(v)).ValueGeneratedNever();
            e.Property(p => p.GoalId).HasConversion(v => v.Value, v => ClimateGoalId.From(v));
            e.Property(p => p.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(p => p.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(p => p.ActiveProfile).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.FailureCode).HasMaxLength(100);
            e.Property(p => p.FailureReason).HasMaxLength(500);
            e.Property(p => p.PlanningReason).HasMaxLength(500);
            e.Property(p => p.CorrelationId).HasMaxLength(100);
            e.Property(p => p.CausationId).HasMaxLength(100);
            e.Property(p => p.IdempotencyKey).HasMaxLength(200);
            e.Property(p => p.Version).IsRequired().IsConcurrencyToken();
            e.Ignore(p => p.DomainEvents);

            e.HasMany(p => p.SubPlans)
                .WithOne()
                .HasForeignKey(sp => sp.ClimatePlanId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.Dependencies)
                .WithOne()
                .HasForeignKey(d => d.ClimatePlanId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.ResolvedConflicts)
                .WithOne()
                .HasForeignKey(c => c.ClimatePlanId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.ResourceReservations)
                .WithOne()
                .HasForeignKey(r => r.ClimatePlanId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.RoomId, p.Status })
                .HasFilter("Status IN ('Created','Planning','Planned','ReservingResources','Ready','Executing','WaitingForEffect','PartiallyCompleted')")
                .IsUnique();
            e.HasIndex(p => p.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.HasIndex(p => new { p.GoalId, p.CreatedAt });
            e.HasIndex(p => new { p.Status, p.CreatedAt });
            e.HasIndex(p => p.CorrelationId);
        });

        mb.Entity<EngineeringSubPlan>(e =>
        {
            e.ToTable("engineering_sub_plans");
            e.HasKey(sp => sp.Id);
            e.Property(sp => sp.EngineeringCapabilityCode).HasMaxLength(50).IsRequired();
            e.Property(sp => sp.PriorityCategory).HasMaxLength(20);
            e.Property(sp => sp.Status).HasConversion<string>().HasMaxLength(25);
            e.Property(sp => sp.EngineeringSystemId).HasMaxLength(50);
            e.Property(sp => sp.EngineeringCommandPlanId).HasMaxLength(50);
            e.Property(sp => sp.IdempotencyKey).HasMaxLength(200);
            e.Property(sp => sp.FailureCode).HasMaxLength(100);
            e.Property(sp => sp.Version).IsRequired().IsConcurrencyToken();
            e.HasIndex(sp => new { sp.ClimatePlanId, sp.Status });
            e.HasIndex(sp => sp.EngineeringCommandPlanId);
            e.HasIndex(sp => sp.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });

        mb.Entity<ClimatePlanDependency>(e =>
        {
            e.ToTable("climate_plan_dependencies");
            e.HasKey(d => d.Id);
            e.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(d => d.Reason).HasMaxLength(500);
            e.HasIndex(d => d.ClimatePlanId);
            e.HasIndex(d => d.PredecessorSubPlanId);
            e.HasIndex(d => d.SuccessorSubPlanId);
        });

        mb.Entity<ClimateConflict>(e =>
        {
            e.ToTable("climate_conflicts");
            e.HasKey(c => c.Id);
            e.Property(c => c.ConflictType).HasConversion<string>().HasMaxLength(30);
            e.Property(c => c.FirstCapabilityCode).HasMaxLength(50);
            e.Property(c => c.SecondCapabilityCode).HasMaxLength(50);
            e.Property(c => c.WinnerCapabilityCode).HasMaxLength(50);
            e.Property(c => c.LoserCapabilityCode).HasMaxLength(50);
            e.Property(c => c.Resolution).HasMaxLength(200);
            e.Property(c => c.Reason).HasMaxLength(500);
            e.HasIndex(c => c.ClimatePlanId);
        });

        mb.Entity<ClimateResource>(e =>
        {
            e.ToTable("climate_resources");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasConversion(v => v.Value, v => ClimateResourceId.From(v)).ValueGeneratedNever();
            e.Property(r => r.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(r => r.ResourceCode).HasMaxLength(50).IsRequired();
            e.Property(r => r.Unit).HasMaxLength(20);
            e.Property(r => r.Version).IsRequired().IsConcurrencyToken();
            e.HasIndex(r => new { r.BuildingId, r.ResourceCode }).IsUnique();
        });

        mb.Entity<ClimateResourceReservation>(e =>
        {
            e.ToTable("climate_resource_reservations");
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasMaxLength(20);
            e.Property(r => r.Version).IsRequired().IsConcurrencyToken();
            e.HasIndex(r => new { r.ClimateResourceId, r.Status });
            e.HasIndex(r => r.ClimatePlanId);
        });

        mb.Entity<ClimateEventInboxEntry>(e =>
        {
            e.ToTable("event_inbox");
            e.HasKey(x => x.Id);
            e.Property(x => x.ConsumerName).HasMaxLength(100).IsRequired();
            e.Property(x => x.EventId).HasMaxLength(100).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.FailureCode).HasMaxLength(100);
            e.HasIndex(x => new { x.ConsumerName, x.EventId }).IsUnique();
            e.HasIndex(x => new { x.Status, x.AvailableAt });
        });

        mb.Entity<ClimateEvaluationEntry>(e =>
        {
            e.ToTable("climate_evaluations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Trigger).HasMaxLength(50);
            e.Property(x => x.Outcome).HasMaxLength(20);
            e.Property(x => x.FailureCode).HasMaxLength(100);
            e.HasIndex(x => new { x.RoomId, x.EvaluatedAt });
            e.HasIndex(x => x.ClimatePlanId);
        });
    }
}