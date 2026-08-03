using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure;

public class EngineeringSystemsDbContext(DbContextOptions<EngineeringSystemsDbContext> options) : DbContext(options)
{
    public DbSet<EngineeringSystem> EngineeringSystems => Set<EngineeringSystem>();
    public DbSet<SystemCapability> Capabilities => Set<SystemCapability>();
    public DbSet<EngineeringResource> Resources => Set<EngineeringResource>();
    public DbSet<CommandPlan> CommandPlans => Set<CommandPlan>();
    public DbSet<CommandPlanStep> CommandPlanSteps => Set<CommandPlanStep>();
    public DbSet<ResourceAllocation> ResourceAllocations => Set<ResourceAllocation>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<SystemZone> Zones => Set<SystemZone>();
    public DbSet<ThermalZone> ThermalZones => Set<ThermalZone>();
    public DbSet<ThermalZoneRoom> ThermalZoneRooms => Set<ThermalZoneRoom>();
    public DbSet<ThermalDemand> ThermalDemands => Set<ThermalDemand>();
    public DbSet<HeatSource> HeatSources => Set<HeatSource>();
    public DbSet<HydraulicCircuit> HydraulicCircuits => Set<HydraulicCircuit>();
    public DbSet<MixingUnit> MixingUnits => Set<MixingUnit>();
    public DbSet<CirculationPump> CirculationPumps => Set<CirculationPump>();
    public DbSet<BufferTank> BufferTanks => Set<BufferTank>();
    public DbSet<DomesticHotWaterSystem> DomesticHotWaterSystems => Set<DomesticHotWaterSystem>();
    public DbSet<WeatherCompensationCurve> WeatherCompensationCurves => Set<WeatherCompensationCurve>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("engineering");

        mb.Entity<EngineeringSystem>(e =>
        {
            e.ToTable("engineering_systems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => EngineeringSystemId.From(v)).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SystemType).HasConversion<string>().HasMaxLength(50);
#pragma warning disable CS0618
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.DeviceIds);
#pragma warning restore CS0618
            e.Property(x => x.ControlMode).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Lifecycle).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.OperationalStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CreatedAt);
            e.Property(x => x.UpdatedAt);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Ignore(x => x.Capabilities);
            e.Ignore(x => x.DeviceBindings);
            e.Ignore(x => x.Resources);
            e.Ignore(x => x.Zones);
            e.Ignore(x => x.HeatSources);
            e.Ignore(x => x.HydraulicCircuits);
            e.Ignore(x => x.VentilationConfiguration);
            e.Ignore(x => x.ThermalConfiguration);
            e.Ignore(x => x.DomainEvents);

            e.HasMany<SystemCapability>().WithOne().HasForeignKey("EngineeringSystemId");
            e.HasMany<EngineeringResource>().WithOne().HasForeignKey("EngineeringSystemId");

            e.HasIndex(x => x.BuildingId);
            e.HasIndex(x => x.SystemType);
        });

        mb.Entity<EngineeringSystemDeviceBinding>(b =>
        {
            b.ToTable("engineering_system_devices");
            b.HasKey(x => x.Id);
            b.Property(x => x.DeviceId);
            b.Property(x => x.Role).HasMaxLength(50).IsRequired();
            b.Property(x => x.Priority);
            b.Property(x => x.Enabled);
            b.Property(x => x.CreatedAt);
            b.Property(x => x.UpdatedAt);
            b.HasIndex(x => x.Role);
            b.HasIndex("EngineeringSystemId");
        });

        mb.Entity<SystemZoneRoom>(zr =>
        {
            zr.ToTable("engineering_zone_rooms");
            zr.HasKey(x => x.Id);
            zr.Property(x => x.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            zr.Property(x => x.CoverageWeight);
            zr.Property(x => x.Priority);
            zr.Property(x => x.Enabled);
            zr.Property(x => x.CreatedAt);
            zr.Property(x => x.UpdatedAt);
            zr.HasIndex(x => x.RoomId);
            zr.HasIndex(x => x.Enabled);
            zr.HasIndex("ZoneId");
        });

        mb.Entity<SystemCapability>(cap =>
        {
            cap.ToTable("engineering_capabilities");
            cap.HasKey(x => x.Id);
            cap.Property(x => x.Code).HasMaxLength(100).IsRequired();
            cap.Property(x => x.DataType).HasMaxLength(50);
            cap.Property(x => x.Unit).HasMaxLength(20);
            cap.HasIndex("EngineeringSystemId");
            cap.HasIndex(x => x.Code);
        });

        mb.Entity<EngineeringResource>(res =>
        {
            res.ToTable("engineering_resources");
            res.HasKey(x => x.Id);
            res.Property(x => x.Code).HasMaxLength(100).IsRequired();
            res.Property(x => x.Unit).HasMaxLength(20);
            res.Property(x => x.Version).IsConcurrencyToken();
            res.Ignore(x => x.CurrentAllocation);
            res.HasIndex("EngineeringSystemId");
            res.HasIndex(x => x.Code);
        });

        mb.Entity<CommandPlan>(p =>
        {
            p.ToTable("command_plans");
            p.HasKey(x => x.Id);
            p.Property(x => x.Id).ValueGeneratedNever();
            p.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            p.Property(x => x.NeedType).HasMaxLength(50);
            p.Property(x => x.CapabilityCode).HasMaxLength(100).IsRequired();
            p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            p.Property(x => x.ValueUnit).HasMaxLength(20);
            p.Property(x => x.StrategyName).HasMaxLength(200);
            p.Property(x => x.FailureCode).HasMaxLength(100);
            p.Property(x => x.NeedId).HasMaxLength(50);
            p.Property(x => x.BuildingId).HasConversion(v => v!.Value.Value, v => BuildingId.From(v));
            p.Property(x => x.RoomId).HasConversion(v => v!.Value.Value, v => RoomId.From(v));
            p.Property(x => x.StrategyStrategyId);
            p.Property(x => x.RequestedEffect).HasMaxLength(500);
            p.Property(x => x.ResourceAllocationId);
            p.Property(x => x.PlannedAt);
            p.Property(x => x.StartedAt);
            p.Property(x => x.FailedAt);
            p.Property(x => x.CancelledAt);
            p.Property(x => x.ExpiresAt);
            p.Property(x => x.CorrelationId).HasMaxLength(100);
            p.Property(x => x.CausationId).HasMaxLength(100);
            p.Property(x => x.IdempotencyKey).HasMaxLength(200);
            p.Ignore(x => x.Steps);
            p.Ignore(x => x.ResourceAllocations);
            p.Ignore(x => x.DomainEvents);

            p.HasMany<CommandPlanStep>().WithOne().HasForeignKey("CommandPlanId");
            p.HasMany<ResourceAllocation>().WithOne().HasForeignKey("CommandPlanId");

            p.HasIndex(x => x.EngineeringSystemId);
            p.HasIndex(x => x.Status);
            p.HasIndex(x => x.NeedId);
            p.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });

        mb.Entity<CommandPlanStep>(step =>
        {
            step.ToTable("command_plan_steps");
            step.HasKey(x => x.Id);
            step.Property(x => x.CapabilityCode).HasMaxLength(100).IsRequired();
            step.Property(x => x.Operation).HasMaxLength(50);
            step.Property(x => x.ValueUnit).HasMaxLength(20);
            step.Property(x => x.DeviceRole).HasMaxLength(50);
            step.Property(x => x.Sequence);
            step.Property(x => x.ExecutionMode).HasConversion<string>().HasMaxLength(20);
            step.Property(x => x.Required);
            step.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            step.Property(x => x.StartedAt);
            step.Property(x => x.CompletedAt);
            step.Property(x => x.FailureCode).HasMaxLength(100);
            step.Ignore(x => x.Version);
            step.HasIndex(x => x.DeviceRole);
        });

        mb.Entity<ResourceAllocation>(alloc =>
        {
            alloc.ToTable("command_plan_resource_allocations");
            alloc.HasKey(x => x.Id);
            alloc.Property(x => x.ResourceCode).HasMaxLength(100).IsRequired();
        });

        mb.Entity<SystemZone>(z =>
        {
            z.ToTable("engineering_zones");
            z.HasKey(x => x.Id);
            z.Property(x => x.Name).HasMaxLength(200).IsRequired();
            z.Property(x => x.Priority);
            z.Property(x => x.PreferredEngineeringSystemId).HasConversion(v => v!.Value.Value, v => EngineeringSystemId.From(v));
#pragma warning disable CS0618
            z.Ignore(x => x.RoomIds);
#pragma warning restore CS0618
            z.Ignore(x => x.CachedRoomIds);

            z.HasMany<SystemZoneRoom>().WithOne().HasForeignKey("ZoneId");
        });

        mb.Entity<Strategy>(s =>
        {
            s.ToTable("engineering_strategies");
            s.HasKey(x => x.Id);
            s.Property(x => x.Id).ValueGeneratedNever();
            s.Property(x => x.Name).HasMaxLength(200).IsRequired();
            s.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            s.Property(x => x.ConfigurationJson).HasColumnType("jsonb");
            s.HasIndex(x => x.Type);
        });

        // ============ Thermal Domain EF Configurations ============

        mb.Entity<ThermalZone>(e =>
        {
            e.ToTable("thermal_zones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => ThermalZoneId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.BuildingId).HasConversion(v => v.Value, v => BuildingId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DemandStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Ignore(x => x.Rooms);
            e.HasMany<ThermalZoneRoom>().WithOne().HasForeignKey("ZoneId");
            e.HasIndex(x => new { x.EngineeringSystemId, x.Status });
            e.HasIndex(x => new { x.BuildingId, x.Status });
        });

        mb.Entity<ThermalZoneRoom>(e =>
        {
            e.ToTable("thermal_zone_rooms");
            e.HasKey(x => x.Id);
            e.Property(x => x.ZoneId).HasConversion(v => v.Value, v => ThermalZoneId.From(v));
            e.Property(x => x.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.RoomId);
            e.HasIndex(x => new { x.ZoneId, x.Enabled });
        });

        mb.Entity<ThermalDemand>(e =>
        {
            e.ToTable("thermal_demands");
            e.HasKey(x => x.Id);
            e.Property(x => x.RoomId).HasConversion(v => v.Value, v => RoomId.From(v));
            e.Property(x => x.DemandType).HasMaxLength(30);
            e.Property(x => x.Severity).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasIndex(x => x.ThermalZoneId);
            e.HasIndex(x => new { x.RoomId, x.Status });
            e.HasIndex(x => x.ClimatePlanId);
        });

        mb.Entity<HeatSource>(e =>
        {
            e.ToTable("heat_sources");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => HeatSourceId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RuntimeState).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DefrostState).HasMaxLength(30);
            e.Property(x => x.FailureCode).HasMaxLength(100);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.EngineeringSystemId, x.LifecycleStatus });
            e.HasIndex(x => new { x.RuntimeState, x.CooldownUntil });
        });

        mb.Entity<HydraulicCircuit>(e =>
        {
            e.ToTable("hydraulic_circuits");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => HydraulicCircuitId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CircuitType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ResourceCode).HasMaxLength(50);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.EngineeringSystemId, x.LifecycleStatus });
            e.HasIndex(x => x.CircuitType);
        });

        mb.Entity<MixingUnit>(e =>
        {
            e.ToTable("mixing_units");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => MixingUnitId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ControlMode).HasMaxLength(30);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.CircuitId);
        });

        mb.Entity<CirculationPump>(e =>
        {
            e.ToTable("circulation_pumps");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => CirculationPumpId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.CircuitId);
        });

        mb.Entity<BufferTank>(e =>
        {
            e.ToTable("buffer_tanks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => BufferTankId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ChargeStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SensorQuality).HasMaxLength(20);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.EngineeringSystemId);
        });

        mb.Entity<DomesticHotWaterSystem>(e =>
        {
            e.ToTable("domestic_hot_water_systems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => DomesticHotWaterSystemId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.PriorityMode).HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.HeatingCircuitId).HasConversion(v => v!.Value.Value, v => HydraulicCircuitId.From(v));
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.EngineeringSystemId).IsUnique();
        });

        mb.Entity<WeatherCompensationCurve>(e =>
        {
            e.ToTable("weather_compensation_curves");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(v => v.Value, v => WeatherCompensationCurveId.From(v)).ValueGeneratedNever();
            e.Property(x => x.EngineeringSystemId).HasConversion(v => v.Value, v => EngineeringSystemId.From(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.EngineeringSystemId);
        });
    }
}

public class EngineeringSystemRepository(EngineeringSystemsDbContext ctx) : IEngineeringSystemRepository
{
    public async Task<EngineeringSystem?> GetByIdAsync(EngineeringSystemId id, CancellationToken ct = default)
    {
        var system = await ctx.EngineeringSystems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (system is null) return null;
        await ctx.Entry(system).Collection(s => ctx.Set<SystemCapability>().Where(c => c.EngineeringSystemId == system.Id.Value).ToList()).LoadAsync(ct);
        await ctx.Entry(system).Collection(s => ctx.Set<EngineeringResource>().Where(r => r.EngineeringSystemId == system.Id.Value).ToList()).LoadAsync(ct);
        return system;
    }

    public async Task<IReadOnlyCollection<EngineeringSystem>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default)
        => await ctx.EngineeringSystems.Where(x => x.BuildingId == buildingId).ToListAsync(ct);

    public async Task<IReadOnlyCollection<EngineeringSystem>> GetByRoomAsync(RoomId roomId, CancellationToken ct = default)
        => await ctx.EngineeringSystems.ToListAsync(ct);

    public async Task<IReadOnlyCollection<EngineeringSystem>> GetByCapabilityAsync(string capabilityCode, CancellationToken ct = default)
        => await ctx.EngineeringSystems
            .Where(x => ctx.Set<SystemCapability>().Any(c => c.EngineeringSystemId == x.Id.Value && c.Code == capabilityCode))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<EngineeringSystem>> GetAllAsync(CancellationToken ct = default)
        => await ctx.EngineeringSystems.ToListAsync(ct);

    public async Task<IReadOnlyCollection<EngineeringSystem>> GetByLifecycleStatusAsync(LifecycleStatus status, CancellationToken ct = default)
        => await ctx.EngineeringSystems.Where(x => x.Lifecycle == status).ToListAsync(ct);

    public async Task<IReadOnlyCollection<EngineeringSystem>> GetByZoneAsync(Guid zoneId, CancellationToken ct = default)
        => await ctx.EngineeringSystems
            .Where(x => ctx.Set<SystemZone>().Any(z => z.Id == zoneId && z.PreferredEngineeringSystemId != null && z.PreferredEngineeringSystemId.Value.Value == x.Id.Value))
            .ToListAsync(ct);

    public async Task AddAsync(EngineeringSystem system, CancellationToken ct = default)
    {
        await ctx.EngineeringSystems.AddAsync(system, ct);
        foreach (var cap in system.Capabilities) cap.EngineeringSystemId = system.Id.Value;
        foreach (var res in system.Resources) res.EngineeringSystemId = system.Id.Value;
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EngineeringSystem system, CancellationToken ct = default)
    {
        ctx.EngineeringSystems.Update(system);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EngineeringSystemId id, CancellationToken ct = default)
    {
        await ctx.Set<SystemCapability>().Where(c => c.EngineeringSystemId == id.Value).ExecuteDeleteAsync(ct);
        await ctx.Set<EngineeringResource>().Where(r => r.EngineeringSystemId == id.Value).ExecuteDeleteAsync(ct);
        await ctx.EngineeringSystems.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyCollection<VentilationDemand>> GetActiveVentilationDemandsAsync(Guid engineeringSystemId, CancellationToken ct = default)
        => await ctx.Set<VentilationDemand>()
            .Where(d => d.EngineeringSystemId == engineeringSystemId && d.IsActive)
            .ToListAsync(ct);
}

public class CommandPlanRepository(EngineeringSystemsDbContext ctx) : ICommandPlanRepository
{
    public async Task<CommandPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ctx.CommandPlans.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<CommandPlan>> GetBySystemAsync(EngineeringSystemId systemId, CancellationToken ct = default)
        => await ctx.CommandPlans.Where(x => x.EngineeringSystemId == systemId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyCollection<CommandPlan>> GetActiveAsync(CancellationToken ct = default)
        => await ctx.CommandPlans
            .Where(x => x.Status == Domain.CommandPlanStatus.Requested
                || x.Status == Domain.CommandPlanStatus.Reserved
                || x.Status == Domain.CommandPlanStatus.Allocated
                || x.Status == Domain.CommandPlanStatus.Executing)
            .ToListAsync(ct);

    public async Task AddAsync(CommandPlan plan, CancellationToken ct = default)
    {
        await ctx.CommandPlans.AddAsync(plan, ct);
        foreach (var s in plan.Steps) s.CommandPlanId = plan.Id;
        foreach (var a in plan.ResourceAllocations) a.CommandPlanId = plan.Id;
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CommandPlan plan, CancellationToken ct = default)
    {
        ctx.CommandPlans.Update(plan);
        await ctx.SaveChangesAsync(ct);
    }
}

public class StrategyRepository(EngineeringSystemsDbContext ctx) : IStrategyRepository
{
    public async Task<Strategy?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ctx.Strategies.FindAsync([id], ct);

    public async Task<IReadOnlyCollection<Strategy>> GetAllAsync(CancellationToken ct = default)
        => await ctx.Strategies.OrderBy(s => s.CreatedAt).ToListAsync(ct);

    public async Task<Strategy?> GetActiveByTypeAsync(StrategyType type, CancellationToken ct = default)
        => await ctx.Strategies.FirstOrDefaultAsync(s => s.Type == type && s.IsActive, ct);

    public async Task AddAsync(Strategy strategy, CancellationToken ct = default)
    {
        await ctx.Strategies.AddAsync(strategy, ct);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Strategy strategy, CancellationToken ct = default)
    {
        ctx.Strategies.Update(strategy);
        await ctx.SaveChangesAsync(ct);
    }
}