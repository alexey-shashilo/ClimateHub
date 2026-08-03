using System.Security.Cryptography;
using System.Text;

namespace ClimateHub.Modules.IAM.Domain;

public class RefreshSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string RefreshToken { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public Guid TokenFamilyId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshSession() { }

    public RefreshSession(Guid id, Guid userId, string refreshToken, Guid tokenFamilyId, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenFamilyId = tokenFamilyId;
        RefreshToken = refreshToken;
        TokenHash = HashToken(refreshToken);
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}