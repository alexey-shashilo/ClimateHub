using ClimateHub.Modules.IAM.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class RefreshSessionRepository : IRefreshSessionRepository
{
    private readonly IamDbContext _db;

    public RefreshSessionRepository(IamDbContext db) => _db = db;

    public Task<RefreshSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = RefreshSession.HashToken(refreshToken);
        return _db.RefreshSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct);
    }

    public Task<List<RefreshSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.RefreshSessions.Where(s => s.UserId == userId).ToListAsync(ct);

    public Task<List<RefreshSession>> GetByFamilyIdAsync(Guid tokenFamilyId, CancellationToken ct = default) =>
        _db.RefreshSessions.Where(s => s.TokenFamilyId == tokenFamilyId).ToListAsync(ct);

    public async Task AddAsync(RefreshSession session, CancellationToken ct = default)
    {
        _db.RefreshSessions.Add(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshSession session, CancellationToken ct = default)
    {
        _db.RefreshSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _db.RefreshSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke();

        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeFamilyAsync(Guid tokenFamilyId, CancellationToken ct = default)
    {
        var sessions = await _db.RefreshSessions
            .Where(s => s.TokenFamilyId == tokenFamilyId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        var expired = await _db.RefreshSessions
            .Where(s => s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        if (expired.Count == 0) return 0;

        _db.RefreshSessions.RemoveRange(expired);
        await _db.SaveChangesAsync(ct);
        return expired.Count;
    }
}