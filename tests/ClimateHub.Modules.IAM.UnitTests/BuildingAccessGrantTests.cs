using ClimateHub.Modules.IAM.Domain;

namespace ClimateHub.Modules.IAM.UnitTests;

public class BuildingAccessGrantTests
{
    [Fact]
    public void BuildingAccessGrant_ShouldCreateWithCorrectValues()
    {
        var userId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();

        var grant = new BuildingAccessGrant(userId, buildingId, grantedBy);

        Assert.Equal(userId, grant.UserId);
        Assert.Equal(buildingId, grant.BuildingId);
        Assert.Equal(grantedBy, grant.GrantedBy);
        Assert.True((DateTime.UtcNow - grant.GrantedAt).TotalSeconds < 5);
    }

    [Fact]
    public void UserAccount_ShouldBeActiveOnCreation()
    {
        var user = new UserAccount(Guid.NewGuid(), "test@test.com", "hash", "Test User");
        Assert.True(user.IsActive);
    }

    [Fact]
    public void UserAccount_Deactivate_ShouldSetIsActiveFalse()
    {
        var user = new UserAccount(Guid.NewGuid(), "test@test.com", "hash", "Test User");
        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void UserAccount_RecordLogin_ShouldUpdateLastLoginAt()
    {
        var user = new UserAccount(Guid.NewGuid(), "test@test.com", "hash", "Test User");
        Assert.Null(user.LastLoginAt);
        user.RecordLogin();
        Assert.NotNull(user.LastLoginAt);
        Assert.True((DateTime.UtcNow - user.LastLoginAt.Value).TotalSeconds < 5);
    }
}
