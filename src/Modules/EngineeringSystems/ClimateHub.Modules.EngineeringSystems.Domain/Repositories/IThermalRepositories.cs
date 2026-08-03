using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Repositories;

public interface IThermalZoneRepository
{
    Task<ThermalZone?> GetByIdAsync(ThermalZoneId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<ThermalZone>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ThermalZone>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ThermalZone>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(ThermalZone zone, CancellationToken ct = default);
    Task UpdateAsync(ThermalZone zone, CancellationToken ct = default);
    Task DeleteAsync(ThermalZoneId id, CancellationToken ct = default);
}

public interface IHeatSourceRepository
{
    Task<HeatSource?> GetByIdAsync(HeatSourceId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<HeatSource>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task<IReadOnlyCollection<HeatSource>> GetAvailableAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task<IReadOnlyCollection<HeatSource>> GetByRuntimeStateAsync(HeatSourceRuntimeState state, CancellationToken ct = default);
    Task AddAsync(HeatSource source, CancellationToken ct = default);
    Task UpdateAsync(HeatSource source, CancellationToken ct = default);
    Task DeleteAsync(HeatSourceId id, CancellationToken ct = default);
}

public interface IHydraulicCircuitRepository
{
    Task<HydraulicCircuit?> GetByIdAsync(HydraulicCircuitId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<HydraulicCircuit>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task<IReadOnlyCollection<HydraulicCircuit>> GetByTypeAsync(HydraulicCircuitType type, CancellationToken ct = default);
    Task AddAsync(HydraulicCircuit circuit, CancellationToken ct = default);
    Task UpdateAsync(HydraulicCircuit circuit, CancellationToken ct = default);
    Task DeleteAsync(HydraulicCircuitId id, CancellationToken ct = default);
}

public interface IMixingUnitRepository
{
    Task<MixingUnit?> GetByIdAsync(MixingUnitId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<MixingUnit>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task AddAsync(MixingUnit unit, CancellationToken ct = default);
    Task UpdateAsync(MixingUnit unit, CancellationToken ct = default);
}

public interface ICirculationPumpRepository
{
    Task<CirculationPump?> GetByIdAsync(CirculationPumpId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<CirculationPump>> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task AddAsync(CirculationPump pump, CancellationToken ct = default);
    Task UpdateAsync(CirculationPump pump, CancellationToken ct = default);
}

public interface IBufferTankRepository
{
    Task<BufferTank?> GetByIdAsync(BufferTankId id, CancellationToken ct = default);
    Task<BufferTank?> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task AddAsync(BufferTank tank, CancellationToken ct = default);
    Task UpdateAsync(BufferTank tank, CancellationToken ct = default);
}

public interface IDomesticHotWaterSystemRepository
{
    Task<DomesticHotWaterSystem?> GetByIdAsync(DomesticHotWaterSystemId id, CancellationToken ct = default);
    Task<DomesticHotWaterSystem?> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task AddAsync(DomesticHotWaterSystem dhw, CancellationToken ct = default);
    Task UpdateAsync(DomesticHotWaterSystem dhw, CancellationToken ct = default);
}

public interface IWeatherCompensationCurveRepository
{
    Task<WeatherCompensationCurve?> GetByIdAsync(WeatherCompensationCurveId id, CancellationToken ct = default);
    Task<WeatherCompensationCurve?> GetByEngineeringSystemAsync(EngineeringSystemId engId, CancellationToken ct = default);
    Task AddAsync(WeatherCompensationCurve curve, CancellationToken ct = default);
    Task UpdateAsync(WeatherCompensationCurve curve, CancellationToken ct = default);
}

public interface IThermalDemandRepository
{
    Task<IReadOnlyCollection<ThermalDemand>> GetByZoneAsync(Guid zoneId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ThermalDemand>> GetActiveByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task AddAsync(ThermalDemand demand, CancellationToken ct = default);
    Task UpdateAsync(ThermalDemand demand, CancellationToken ct = default);
}