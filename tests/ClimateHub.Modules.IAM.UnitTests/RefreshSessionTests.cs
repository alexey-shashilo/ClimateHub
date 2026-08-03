using ClimateHub.Modules.IAM.Domain;

namespace ClimateHub.Modules.IAM.UnitTests;

public class RefreshSessionTests
{
    [Fact]
    public void RefreshSession_ShouldNotBeExpiredOnCreation()
    {
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        Assert.False(session.IsExpired);
        Assert.False(session.IsRevoked);
    }

    [Fact]
    public void RefreshSession_ShouldBeExpiredWhenPastExpiry()
    {
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(-1));
        Assert.True(session.IsExpired);
    }

    [Fact]
    public void Revoke_ShouldMarkAsRevoked()
    {
        var session = new RefreshSession(Guid.NewGuid(), Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        session.Revoke();
        Assert.True(session.IsRevoked);
        Assert.NotNull(session.RevokedAt);
    }
}