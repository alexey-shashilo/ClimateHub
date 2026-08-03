namespace ClimateHub.Modules.IAM.Domain;

public interface IBuildingAccessGrantRepository
{
    Task<List<BuildingAccessGrant>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<BuildingAccessGrant>> GetByBuildingIdAsync(Guid buildingId, CancellationToken ct = default);
    Task<bool> HasAccessAsync(Guid userId, Guid buildingId, CancellationToken ct = default);
    Task AddAsync(BuildingAccessGrant grant, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, Guid buildingId, CancellationToken ct = default);
}