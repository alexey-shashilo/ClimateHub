using ClimateHub.Modules.IAM.Domain;

namespace ClimateHub.Modules.IAM.UnitTests;

public class PermissionTests
{
    [Fact]
    public void Role_WithPermissions_ShouldContainExpectedPermissions()
    {
        var role = new Role(Guid.NewGuid(), "building-manager", "Manages buildings",
            new List<Permission>
            {
                Permission.building_read,
                Permission.building_configure,
                Permission.room_read,
                Permission.room_configure
            });

        Assert.True(role.HasPermission(Permission.building_read));
        Assert.True(role.HasPermission(Permission.building_configure));
        Assert.False(role.HasPermission(Permission.device_read));
    }

    [Fact]
    public void Role_UpdatePermissions_ShouldReplacePermissions()
    {
        var role = new Role(Guid.NewGuid(), "viewer", null,
            new List<Permission> { Permission.building_read });

        role.UpdatePermissions(new List<Permission>
        {
            Permission.room_read,
            Permission.environment_read
        });

        Assert.False(role.HasPermission(Permission.building_read));
        Assert.True(role.HasPermission(Permission.room_read));
        Assert.True(role.HasPermission(Permission.environment_read));
    }

    [Fact]
    public void HasAnyPermission_ShouldReturnTrueWhenAnyMatches()
    {
        var role = new Role(Guid.NewGuid(), "viewer", null,
            new List<Permission> { Permission.building_read, Permission.room_read });

        Assert.True(role.HasAnyPermission(new[] { Permission.building_read, Permission.admin }));
        Assert.False(role.HasAnyPermission(new[] { Permission.admin, Permission.device_configure }));
    }
}
