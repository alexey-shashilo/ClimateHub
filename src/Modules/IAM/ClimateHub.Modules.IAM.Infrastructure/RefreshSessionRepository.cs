using ClimateHub.Modules.IAM.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class RefreshSessionRepository : IRefreshSessionRepository
{
    private readonly IamDbContext _db;

    public RefreshSessionRepository(IamDbContext db) => _db = db;

    public Task<RefreshSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
        _db.RefreshSessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, ct);

    public Task<List<RefreshSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.RefreshSessions.Where(s => s.UserId == userId).ToListAsync(ct);

    public Task AddAsync(RefreshSession session, CancellationToken ct = default)
    {
        _db.RefreshSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RefreshSession session, CancellationToken ct = default)
    {
        _db.RefreshSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _db.RefreshSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke();
    }
}