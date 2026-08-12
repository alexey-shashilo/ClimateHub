using System.Security.Cryptography;
using System.Text;

namespace ClimateHub.Modules.IAM.Domain;

public class RefreshSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public Guid? ParentSessionId { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public int Version { get; private set; }

    private RefreshSession() { }

    public RefreshSession(
        Guid id,
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAt,
        Guid? parentSessionId = null,
        string? createdByIp = null)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ParentSessionId = parentSessionId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        CreatedByIp = createdByIp;
        Version = 1;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsConsumed => ConsumedAt is not null;

    public bool IsActive => !IsExpired && !IsRevoked && !IsConsumed;

    public void MarkConsumed(Guid replacedBySessionId)
    {
        ConsumedAt = DateTime.UtcNow;
        ReplacedBySessionId = replacedBySessionId;
    }

    public void Revoke(string? reason = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }

    public void MarkUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
