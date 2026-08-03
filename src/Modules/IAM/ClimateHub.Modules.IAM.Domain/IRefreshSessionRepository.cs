namespace ClimateHub.Modules.IAM.Domain;

public interface IRefreshSessionRepository
{
    Task<RefreshSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<List<RefreshSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<RefreshSession>> GetByFamilyIdAsync(Guid tokenFamilyId, CancellationToken ct = default);
    Task AddAsync(RefreshSession session, CancellationToken ct = default);
    Task UpdateAsync(RefreshSession session, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid tokenFamilyId, CancellationToken ct = default);
    Task<int> CleanupExpiredAsync(CancellationToken ct = default);
}