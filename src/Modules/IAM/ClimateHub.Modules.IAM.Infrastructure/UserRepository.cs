using ClimateHub.Modules.IAM.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly IamDbContext _db;

    public UserRepository(IamDbContext db) => _db = db;

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<List<UserAccount>> GetAllAsync(CancellationToken ct = default) =>
        _db.Users.ToListAsync(ct);

    public async Task AddAsync(UserAccount user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserAccount user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is not null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<Role>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var roleIds = await _db.UserRoleAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.RoleId)
            .ToListAsync(ct);

        return await _db.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);
    }
}