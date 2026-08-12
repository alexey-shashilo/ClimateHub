using ClimateHub.Modules.IAM.Domain;

namespace ClimateHub.Modules.IAM.UnitTests;

public class RefreshSessionTests
{
    private static (string raw, string hash) GenerateToken()
    {
        var raw = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        return (raw, RefreshSession.HashToken(raw));
    }

    [Fact]
    public void RefreshSession_ShouldNotBeExpiredOnCreation()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.False(session.IsExpired);
        Assert.False(session.IsRevoked);
        Assert.False(session.IsConsumed);
        Assert.True(session.IsActive);
    }

    [Fact]
    public void RefreshSession_ShouldBeExpiredWhenPastExpiry()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        Assert.True(session.IsExpired);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void Revoke_ShouldMarkAsRevokedWithReason()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        session.Revoke("USER_REVOKED");
        Assert.True(session.IsRevoked);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("USER_REVOKED", session.RevocationReason);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void MarkConsumed_ShouldSetConsumedAtAndReplacedBy()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        var replacementId = Guid.NewGuid();
        session.MarkConsumed(replacementId);
        Assert.True(session.IsConsumed);
        Assert.NotNull(session.ConsumedAt);
        Assert.Equal(replacementId, session.ReplacedBySessionId);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void TokenHash_ShouldBeSha256Hex()
    {
        const string token = "test-refresh-token-value";
        var hash = RefreshSession.HashToken(token);
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[a-f0-9]{64}$", hash);
    }

    [Fact]
    public void TokenHash_ShouldBeDeterministic()
    {
        const string token = "some-token";
        var hash1 = RefreshSession.HashToken(token);
        var hash2 = RefreshSession.HashToken(token);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TokenHash_DifferentTokens_ShouldProduceDifferentHashes()
    {
        var hash1 = RefreshSession.HashToken("token-a");
        var hash2 = RefreshSession.HashToken("token-b");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void RefreshSession_ShouldStoreFamilyId()
    {
        var (raw, hash) = GenerateToken();
        var familyId = Guid.NewGuid();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, familyId, DateTime.UtcNow.AddDays(7));
        Assert.Equal(familyId, session.FamilyId);
    }

    [Fact]
    public void RefreshSession_ShouldStoreHashedToken()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.Equal(hash, session.TokenHash);
        Assert.NotEqual(raw, session.TokenHash);
    }

    [Fact]
    public void TokenHash_ShouldNotBeReversibleToOriginal()
    {
        const string token = "sensitive-refresh-token";
        var hash = RefreshSession.HashToken(token);
        Assert.DoesNotContain(token, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void RefreshSession_ShouldSupportParentSessionId()
    {
        var (raw, hash) = GenerateToken();
        var parentId = Guid.NewGuid();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(),
            DateTime.UtcNow.AddDays(7), parentSessionId: parentId);
        Assert.Equal(parentId, session.ParentSessionId);
    }

    [Fact]
    public void RefreshSession_ShouldSupportCreatedByIp()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(),
            DateTime.UtcNow.AddDays(7), createdByIp: "192.168.1.1");
        Assert.Equal("192.168.1.1", session.CreatedByIp);
    }

    [Fact]
    public void MarkUsed_ShouldUpdateLastUsedAt()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        session.MarkUsed();
        Assert.NotNull(session.LastUsedAt);
    }

    [Fact]
    public void ConsumedSession_IsNotActive()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        session.MarkConsumed(Guid.NewGuid());
        Assert.False(session.IsActive);
        Assert.True(session.IsConsumed);
    }

    [Fact]
    public void RevokedSession_IsNotActive()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        session.Revoke("TEST_REVOKE");
        Assert.False(session.IsActive);
        Assert.True(session.IsRevoked);
    }

    [Fact]
    public void FullRotationChain_ShouldTrackParentAndReplacement()
    {
        var (raw1, hash1) = GenerateToken();
        var familyId = Guid.NewGuid();
        var session1 = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash1, familyId, DateTime.UtcNow.AddDays(7));

        var (raw2, hash2) = GenerateToken();
        var session2 = new RefreshSession(Guid.NewGuid(), session1.UserId, hash2, familyId,
            DateTime.UtcNow.AddDays(7), parentSessionId: session1.Id);

        session1.MarkConsumed(session2.Id);
        session2.MarkConsumed(Guid.NewGuid());

        Assert.Equal(session2.Id, session1.ReplacedBySessionId);
        Assert.Equal(session1.Id, session2.ParentSessionId);
        Assert.True(session1.IsConsumed);
        Assert.True(session2.IsConsumed);
    }

    [Fact]
    public void MultipleRevocations_ShouldNotChangeFirstRevokedAt()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        session.Revoke("FIRST");
        var firstRevokedAt = session.RevokedAt!.Value;
        session.Revoke("SECOND");
        Assert.Equal(firstRevokedAt, session.RevokedAt!.Value, TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void Version_ShouldStartAtOne()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.Equal(1, session.Version);
    }

    [Fact]
    public void RawToken_DiffersFromStoredHash()
    {
        var (raw, hash) = GenerateToken();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), hash, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.NotEqual(raw, session.TokenHash);
        Assert.Equal(hash, session.TokenHash);
    }
}
