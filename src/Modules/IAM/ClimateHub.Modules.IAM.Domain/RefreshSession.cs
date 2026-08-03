namespace ClimateHub.Modules.IAM.Domain;

public class RefreshSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshSession() { }

    public RefreshSession(Guid id, Guid userId, string refreshToken, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}