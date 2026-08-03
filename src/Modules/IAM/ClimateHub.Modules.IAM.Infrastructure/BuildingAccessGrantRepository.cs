using ClimateHub.Modules.IAM.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class BuildingAccessGrantRepository : IBuildingAccessGrantRepository
{
    private readonly IamDbContext _db;

    public BuildingAccessGrantRepository(IamDbContext db) => _db = db;

    public Task<List<BuildingAccessGrant>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.BuildingAccessGrants.Where(g => g.UserId == userId).ToListAsync(ct);

    public Task<List<BuildingAccessGrant>> GetByBuildingIdAsync(Guid buildingId, CancellationToken ct = default) =>
        _db.BuildingAccessGrants.Where(g => g.BuildingId == buildingId).ToListAsync(ct);

    public Task<bool> HasAccessAsync(Guid userId, Guid buildingId, CancellationToken ct = default) =>
        _db.BuildingAccessGrants.AnyAsync(g => g.UserId == userId && g.BuildingId == buildingId, ct);

    public Task AddAsync(BuildingAccessGrant grant, CancellationToken ct = default)
    {
        _db.BuildingAccessGrants.Add(grant);
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(Guid userId, Guid buildingId, CancellationToken ct = default)
    {
        var grant = await _db.BuildingAccessGrants
            .FirstOrDefaultAsync(g => g.UserId == userId && g.BuildingId == buildingId, ct);
        if (grant is not null)
            _db.BuildingAccessGrants.Remove(grant);
    }
}