using ClimateHub.Modules.IAM.Domain;

namespace ClimateHub.Modules.IAM.UnitTests;

public class RefreshSessionTests
{
    [Fact]
    public void RefreshSession_ShouldNotBeExpiredOnCreation()
    {
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "token", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.False(session.IsExpired);
        Assert.False(session.IsRevoked);
    }

    [Fact]
    public void RefreshSession_ShouldBeExpiredWhenPastExpiry()
    {
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "token", Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        Assert.True(session.IsExpired);
    }

    [Fact]
    public void Revoke_ShouldMarkAsRevoked()
    {
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "token", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        session.Revoke();
        Assert.True(session.IsRevoked);
        Assert.NotNull(session.RevokedAt);
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
    public void RefreshSession_ShouldStoreTokenFamilyId()
    {
        var familyId = Guid.NewGuid();
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "some-token", familyId, DateTime.UtcNow.AddDays(7));
        Assert.Equal(familyId, session.TokenFamilyId);
    }

    [Fact]
    public void RefreshSession_ShouldStoreHashedToken()
    {
        const string token = "my-refresh-token";
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), token, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.Equal(RefreshSession.HashToken(token), session.TokenHash);
        Assert.NotEqual(token, session.TokenHash);
    }

    [Fact]
    public void TokenHash_ShouldNotBeReversibleToOriginal()
    {
        const string token = "sensitive-refresh-token";
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), token, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        Assert.DoesNotContain(token, session.TokenHash, StringComparison.Ordinal);
    }
}