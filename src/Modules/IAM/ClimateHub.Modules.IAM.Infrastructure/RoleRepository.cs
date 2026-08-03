using ClimateHub.Modules.IAM.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class RoleRepository : IRoleRepository
{
    private readonly IamDbContext _db;

    public RoleRepository(IamDbContext db) => _db = db;

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<List<Role>> GetAllAsync(CancellationToken ct = default) =>
        _db.Roles.ToListAsync(ct);

    public Task AddAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Update(role);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is not null)
            _db.Roles.Remove(role);
    }
}