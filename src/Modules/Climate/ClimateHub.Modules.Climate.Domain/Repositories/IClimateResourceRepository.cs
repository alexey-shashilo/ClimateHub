using ClimateHub.Modules.Climate.Domain.Resources;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Domain.Repositories;

public interface IClimateResourceRepository
{
    Task<ClimateResource?> GetByCodeAsync(BuildingId buildingId, string code, CancellationToken ct = default);
    Task<IReadOnlyCollection<ClimateResource>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default);
    Task AddAsync(ClimateResource resource, CancellationToken ct = default);
    Task UpdateAsync(ClimateResource resource, CancellationToken ct = default);
    Task<bool> DeleteAsync(ClimateResourceId id, CancellationToken ct = default);
}