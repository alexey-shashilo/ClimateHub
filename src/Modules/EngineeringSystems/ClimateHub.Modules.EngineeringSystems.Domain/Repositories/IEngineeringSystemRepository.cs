using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain.Repositories;

public interface IEngineeringSystemRepository
{
    Task<EngineeringSystem?> GetByIdAsync(EngineeringSystemId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<EngineeringSystem>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default);
    Task<IReadOnlyCollection<EngineeringSystem>> GetByRoomAsync(RoomId roomId, CancellationToken ct = default);
    Task<IReadOnlyCollection<EngineeringSystem>> GetByCapabilityAsync(string capabilityCode, CancellationToken ct = default);
    Task<IReadOnlyCollection<EngineeringSystem>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<EngineeringSystem>> GetByLifecycleStatusAsync(LifecycleStatus status, CancellationToken ct = default);
    Task<IReadOnlyCollection<EngineeringSystem>> GetByZoneAsync(Guid zoneId, CancellationToken ct = default);
    Task AddAsync(EngineeringSystem system, CancellationToken ct = default);
    Task UpdateAsync(EngineeringSystem system, CancellationToken ct = default);
    Task DeleteAsync(EngineeringSystemId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<VentilationDemand>> GetActiveVentilationDemandsAsync(Guid engineeringSystemId, CancellationToken ct = default);
}

public interface ICommandPlanRepository
{
    Task<CommandPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommandPlan>> GetBySystemAsync(EngineeringSystemId systemId, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommandPlan>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(CommandPlan plan, CancellationToken ct = default);
    Task UpdateAsync(CommandPlan plan, CancellationToken ct = default);
}

public interface IStrategyRepository
{
    Task<Strategy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyCollection<Strategy>> GetAllAsync(CancellationToken ct = default);
    Task<Strategy?> GetActiveByTypeAsync(StrategyType type, CancellationToken ct = default);
    Task AddAsync(Strategy strategy, CancellationToken ct = default);
    Task UpdateAsync(Strategy strategy, CancellationToken ct = default);
}