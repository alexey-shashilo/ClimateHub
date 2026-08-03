namespace ClimateHub.Modules.IAM.Domain;

public interface IRefreshSessionRepository
{
    Task<RefreshSession?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<List<RefreshSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<RefreshSession>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken ct = default);
    Task<List<RefreshSession>> GetFamilyAsync(Guid familyId, CancellationToken ct = default);
    Task AddAsync(RefreshSession session, CancellationToken ct = default);
    Task UpdateAsync(RefreshSession session, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid familyId, string? reason = null, CancellationToken ct = default);
    Task<int> CleanupExpiredAsync(CancellationToken ct = default);
}