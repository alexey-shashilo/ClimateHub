using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClimateHub.Modules.IAM.Domain;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace ClimateHub.Modules.IAM.UnitTests;

public class JwtTokenTests
{
    private const string SigningKey = "this-is-a-test-signing-key-that-is-at-least-32-chars!!";
    private readonly JwtOptions _jwtOptions;

    public JwtTokenTests()
    {
        _jwtOptions = new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningKey = SigningKey,
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
    }

    [Fact]
    public void GenerateAccessToken_ShouldCreateValidToken()
    {
        var user = new UserAccount(Guid.NewGuid(), "test@test.com", "hash", "Test User");
        var roles = new List<Role> { new(Guid.NewGuid(), "admin", null, new List<Permission> { Permission.admin }) };
        var permissions = roles.SelectMany(r => r.Permissions).Distinct().ToList();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new("security_stamp", user.SecurityStamp)
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission.ToString()));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        Assert.False(string.IsNullOrEmpty(tokenString));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParameters, out _);
        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void ValidateToken_WithWrongKey_ShouldThrow()
    {
        var user = new UserAccount(Guid.NewGuid(), "test@test.com", "hash", "Test User");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this-is-a-completely-different-key-that-is-32-chars!"));
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = wrongKey,
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParameters, out _));
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ShouldFail()
    {
        var user = new UserAccount(Guid.NewGuid(), "test@test.com", "hash", "Test User");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParameters, out _));
    }
}
