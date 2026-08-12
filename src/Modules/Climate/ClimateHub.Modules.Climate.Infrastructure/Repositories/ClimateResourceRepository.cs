using ClimateHub.Modules.Climate.Domain.Resources;
using ClimateHub.Modules.Climate.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Climate.Infrastructure.Repositories;

public class ClimateResourceRepository : IClimateResourceRepository
{
    private readonly ClimateDbContext _db;

    public ClimateResourceRepository(ClimateDbContext db) { _db = db; }

    public async Task<ClimateResource?> GetByCodeAsync(BuildingId buildingId, string code, CancellationToken ct = default) =>
        await _db.ClimateResources.FirstOrDefaultAsync(r => r.BuildingId == buildingId && r.ResourceCode == code, ct);

    public async Task<IReadOnlyCollection<ClimateResource>> GetByBuildingAsync(BuildingId buildingId, CancellationToken ct = default) =>
        await _db.ClimateResources.Where(r => r.BuildingId == buildingId).ToListAsync(ct);

    public async Task AddAsync(ClimateResource resource, CancellationToken ct = default) { await _db.ClimateResources.AddAsync(resource, ct); await _db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(ClimateResource resource, CancellationToken ct = default) { _db.ClimateResources.Update(resource); await _db.SaveChangesAsync(ct); }
    public async Task<bool> DeleteAsync(ClimateResourceId id, CancellationToken ct = default) { var r = await _db.ClimateResources.FindAsync([id], ct); if (r is null) return false; _db.ClimateResources.Remove(r); await _db.SaveChangesAsync(ct); return true; }
}
