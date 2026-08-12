using ClimateHub.Modules.IAM.Application;
using ClimateHub.Modules.IAM.Infrastructure;

namespace ClimateHub.Modules.IAM.UnitTests;

public class PasswordHashingTests
{
    private readonly PasswordHasherService _sut = new();

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        var hash = _sut.HashPassword("TestPassword123!");
        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public void VerifyHashedPassword_WithCorrectPassword_ShouldReturnSuccess()
    {
        var password = "SecureP@ssw0rd";
        var hash = _sut.HashPassword(password);
        var result = _sut.VerifyHashedPassword(hash, password);
        Assert.Equal(PasswordHashResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_WithWrongPassword_ShouldReturnFailed()
    {
        var hash = _sut.HashPassword("CorrectPassword");
        var result = _sut.VerifyHashedPassword(hash, "WrongPassword");
        Assert.Equal(PasswordHashResult.Failed, result);
    }

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashesForSamePassword()
    {
        var password = "SamePassword";
        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);
        Assert.NotEqual(hash1, hash2);
    }
}
