using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Humidification;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure.Repositories;

public class ThermalZoneRepository(EngineeringSystemsDbContext ctx) : IThermalZoneRepository
{
    public async Task<ThermalZone?> GetByIdAsync(ThermalZoneId id, CancellationToken ct = default)
        => await ctx.ThermalZones
            .Include(z => z.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<ThermalZone>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.ThermalZones.Where(x => x.EngineeringSystemId == engId).Include(z => z.Rooms).ToListAsync(ct);

    public async Task<IReadOnlyCollection<ThermalZone>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default)
        => await ctx.ThermalZones.Where(x => x.BuildingId == buildingId).Include(z => z.Rooms).ToListAsync(ct);

    public async Task<IReadOnlyCollection<ThermalZone>> GetActiveAsync(CancellationToken ct = default)
        => await ctx.ThermalZones.Where(x => x.Status == ThermalZoneStatus.Active).Include(z => z.Rooms).ToListAsync(ct);

    public async Task AddAsync(ThermalZone zone, CancellationToken ct = default) { await ctx.ThermalZones.AddAsync(zone, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(ThermalZone zone, CancellationToken ct = default) { ctx.ThermalZones.Update(zone); await ctx.SaveChangesAsync(ct); }
    public async Task DeleteAsync(ThermalZoneId id, CancellationToken ct = default)
    {
        await ctx.ThermalZoneRooms.Where(r => r.ZoneId == id).ExecuteDeleteAsync(ct);
        await ctx.ThermalZones.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }
}

public class HeatSourceRepository(EngineeringSystemsDbContext ctx) : IHeatSourceRepository
{
    public async Task<HeatSource?> GetByIdAsync(HeatSourceId id, CancellationToken ct = default)
        => await ctx.HeatSources.FindAsync([id], ct);

    public async Task<IReadOnlyCollection<HeatSource>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.HeatSources.Where(x => x.EngineeringSystemId == engId).OrderBy(x => x.Priority).ToListAsync(ct);

    public async Task<IReadOnlyCollection<HeatSource>> GetAvailableAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.HeatSources
            .Where(x => x.EngineeringSystemId == engId
                && x.LifecycleStatus == LifecycleStatus.Active
                && x.RuntimeState != HeatSourceRuntimeState.Faulted
                && x.RuntimeState != HeatSourceRuntimeState.Unavailable)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<HeatSource>> GetByRuntimeStateAsync(HeatSourceRuntimeState state, CancellationToken ct = default)
        => await ctx.HeatSources.Where(x => x.RuntimeState == state).ToListAsync(ct);

    public async Task AddAsync(HeatSource source, CancellationToken ct = default) { await ctx.HeatSources.AddAsync(source, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(HeatSource source, CancellationToken ct = default) { ctx.HeatSources.Update(source); await ctx.SaveChangesAsync(ct); }
    public async Task DeleteAsync(HeatSourceId id, CancellationToken ct = default) { await ctx.HeatSources.Where(x => x.Id == id).ExecuteDeleteAsync(ct); }
}

public class HydraulicCircuitRepository(EngineeringSystemsDbContext ctx) : IHydraulicCircuitRepository
{
    public async Task<HydraulicCircuit?> GetByIdAsync(HydraulicCircuitId id, CancellationToken ct = default)
        => await ctx.HydraulicCircuits.FindAsync([id], ct);

    public async Task<IReadOnlyCollection<HydraulicCircuit>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.HydraulicCircuits.Where(x => x.EngineeringSystemId == engId).OrderBy(x => x.Priority).ToListAsync(ct);

    public async Task<IReadOnlyCollection<HydraulicCircuit>> GetByTypeAsync(HydraulicCircuitType type, CancellationToken ct = default)
        => await ctx.HydraulicCircuits.Where(x => x.CircuitType == type).ToListAsync(ct);

    public async Task AddAsync(HydraulicCircuit circuit, CancellationToken ct = default) { await ctx.HydraulicCircuits.AddAsync(circuit, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(HydraulicCircuit circuit, CancellationToken ct = default) { ctx.HydraulicCircuits.Update(circuit); await ctx.SaveChangesAsync(ct); }
    public async Task DeleteAsync(HydraulicCircuitId id, CancellationToken ct = default) { await ctx.HydraulicCircuits.Where(x => x.Id == id).ExecuteDeleteAsync(ct); }
}

public class MixingUnitRepository(EngineeringSystemsDbContext ctx) : IMixingUnitRepository
{
    public async Task<MixingUnit?> GetByIdAsync(MixingUnitId id, CancellationToken ct = default)
        => await ctx.MixingUnits.FindAsync([id], ct);

    public async Task<IReadOnlyCollection<MixingUnit>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.MixingUnits.Where(x => x.EngineeringSystemId == engId).ToListAsync(ct);

    public async Task AddAsync(MixingUnit unit, CancellationToken ct = default) { await ctx.MixingUnits.AddAsync(unit, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(MixingUnit unit, CancellationToken ct = default) { ctx.MixingUnits.Update(unit); await ctx.SaveChangesAsync(ct); }
}

public class CirculationPumpRepository(EngineeringSystemsDbContext ctx) : ICirculationPumpRepository
{
    public async Task<CirculationPump?> GetByIdAsync(CirculationPumpId id, CancellationToken ct = default)
        => await ctx.CirculationPumps.FindAsync([id], ct);

    public async Task<IReadOnlyCollection<CirculationPump>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.CirculationPumps.Where(x => x.EngineeringSystemId == engId).ToListAsync(ct);

    public async Task AddAsync(CirculationPump pump, CancellationToken ct = default) { await ctx.CirculationPumps.AddAsync(pump, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(CirculationPump pump, CancellationToken ct = default) { ctx.CirculationPumps.Update(pump); await ctx.SaveChangesAsync(ct); }
}

public class BufferTankRepository(EngineeringSystemsDbContext ctx) : IBufferTankRepository
{
    public async Task<BufferTank?> GetByIdAsync(BufferTankId id, CancellationToken ct = default)
        => await ctx.BufferTanks.FindAsync([id], ct);

    public async Task<BufferTank?> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.BufferTanks.FirstOrDefaultAsync(x => x.EngineeringSystemId == engId, ct);

    public async Task AddAsync(BufferTank tank, CancellationToken ct = default) { await ctx.BufferTanks.AddAsync(tank, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(BufferTank tank, CancellationToken ct = default) { ctx.BufferTanks.Update(tank); await ctx.SaveChangesAsync(ct); }
}

public class DomesticHotWaterSystemRepository(EngineeringSystemsDbContext ctx) : IDomesticHotWaterSystemRepository
{
    public async Task<DomesticHotWaterSystem?> GetByIdAsync(DomesticHotWaterSystemId id, CancellationToken ct = default)
        => await ctx.DomesticHotWaterSystems.FindAsync([id], ct);

    public async Task<DomesticHotWaterSystem?> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.DomesticHotWaterSystems.FirstOrDefaultAsync(x => x.EngineeringSystemId == engId, ct);

    public async Task AddAsync(DomesticHotWaterSystem dhw, CancellationToken ct = default) { await ctx.DomesticHotWaterSystems.AddAsync(dhw, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(DomesticHotWaterSystem dhw, CancellationToken ct = default) { ctx.DomesticHotWaterSystems.Update(dhw); await ctx.SaveChangesAsync(ct); }
}

public class WeatherCompensationCurveRepository(EngineeringSystemsDbContext ctx) : IWeatherCompensationCurveRepository
{
    public async Task<WeatherCompensationCurve?> GetByIdAsync(WeatherCompensationCurveId id, CancellationToken ct = default)
        => await ctx.WeatherCompensationCurves.FindAsync([id], ct);

    public async Task<WeatherCompensationCurve?> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default)
        => await ctx.WeatherCompensationCurves.FirstOrDefaultAsync(x => x.EngineeringSystemId == engId && x.Enabled, ct);

    public async Task AddAsync(WeatherCompensationCurve curve, CancellationToken ct = default) { await ctx.WeatherCompensationCurves.AddAsync(curve, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(WeatherCompensationCurve curve, CancellationToken ct = default) { ctx.WeatherCompensationCurves.Update(curve); await ctx.SaveChangesAsync(ct); }
}

public class ThermalDemandRepository(EngineeringSystemsDbContext ctx) : IThermalDemandRepository
{
    public async Task<IReadOnlyCollection<ThermalDemand>> GetByZoneAsync(Guid zoneId, CancellationToken ct = default)
        => await ctx.ThermalDemands.Where(d => d.ThermalZoneId == zoneId && d.Status == "Active").ToListAsync(ct);

    public async Task<IReadOnlyCollection<ThermalDemand>> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default)
        => await ctx.ThermalDemands.Where(d => d.RoomId == roomId && d.Status == "Active").ToListAsync(ct);

    public async Task AddAsync(ThermalDemand demand, CancellationToken ct = default) { await ctx.ThermalDemands.AddAsync(demand, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(ThermalDemand demand, CancellationToken ct = default) { ctx.ThermalDemands.Update(demand); await ctx.SaveChangesAsync(ct); }
}

public class HumidificationConfigurationRepository(EngineeringSystemsDbContext ctx) : IHumidificationConfigurationRepository
{
    public async Task<HumidificationSystemConfiguration?> GetByEngineeringSystemAsync(Guid engineeringSystemId, CancellationToken ct = default)
        => await ctx.HumidificationConfigurations.FirstOrDefaultAsync(x => x.EngineeringSystemId == engineeringSystemId, ct);

    public async Task AddAsync(HumidificationSystemConfiguration config, CancellationToken ct = default) { await ctx.HumidificationConfigurations.AddAsync(config, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(HumidificationSystemConfiguration config, CancellationToken ct = default) { ctx.HumidificationConfigurations.Update(config); await ctx.SaveChangesAsync(ct); }
}

public class HumidificationZoneRepository(EngineeringSystemsDbContext ctx) : IHumidificationZoneRepository
{
    public async Task<IReadOnlyCollection<HumidificationZone>> GetByEngineeringSystemAsync(Guid engineeringSystemId, CancellationToken ct = default)
        => await ctx.HumidificationZones.Where(x => x.EngineeringSystemId == engineeringSystemId).ToListAsync(ct);

    public async Task AddAsync(HumidificationZone zone, CancellationToken ct = default) { await ctx.HumidificationZones.AddAsync(zone, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(HumidificationZone zone, CancellationToken ct = default) { ctx.HumidificationZones.Update(zone); await ctx.SaveChangesAsync(ct); }
}

public class HumidificationDemandRepository(EngineeringSystemsDbContext ctx) : IHumidificationDemandRepository
{
    public async Task<IReadOnlyCollection<HumidificationDemand>> GetActiveByEngineeringSystemAsync(Guid engineeringSystemId, CancellationToken ct = default)
        => await ctx.HumidificationDemands.Where(d => d.EngineeringSystemId == engineeringSystemId && d.Status == "Active").ToListAsync(ct);

    public async Task<IReadOnlyCollection<HumidificationDemand>> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default)
        => await ctx.HumidificationDemands.Where(d => d.RoomId == roomId && d.Status == "Active").ToListAsync(ct);

    public async Task AddAsync(HumidificationDemand demand, CancellationToken ct = default) { await ctx.HumidificationDemands.AddAsync(demand, ct); await ctx.SaveChangesAsync(ct); }
    public async Task UpdateAsync(HumidificationDemand demand, CancellationToken ct = default) { ctx.HumidificationDemands.Update(demand); await ctx.SaveChangesAsync(ct); }
}
