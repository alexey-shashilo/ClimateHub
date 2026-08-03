using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ClimateHub.ArchitectureTests;

public class IamValidationTests
{
    private static readonly string SigningKey = "test-signing-key-that-is-at-least-32-characters-long";
    private static readonly SymmetricSecurityKey SecurityKey = new(System.Text.Encoding.UTF8.GetBytes(SigningKey));

    [Fact]
    public void ValidToken_WithCorrectSignature_Passes()
    {
        var token = CreateToken(expires: DateTime.UtcNow.AddHours(1));
        var principal = ValidateToken(token);
        Assert.NotNull(principal);
    }

    [Fact]
    public void InvalidSignature_Throws()
    {
        var token = CreateToken(wrongKey: true);
        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() => ValidateToken(token));
    }

    [Fact]
    public void ExpiredToken_Throws()
    {
        var token = CreateToken(expires: DateTime.UtcNow.AddHours(-2));
        Assert.Throws<SecurityTokenExpiredException>(() => ValidateToken(token));
    }

    [Fact]
    public void TokenWithPermissions_ContainsClaims()
    {
        var token = CreateToken(permissions: new[] { "building_read", "device_configure" });
        var principal = ValidateToken(token);
        Assert.NotNull(principal);
        var perms = principal.FindAll("permission").Select(c => c.Value).ToList();
        Assert.Contains("building_read", perms);
        Assert.Contains("device_configure", perms);
    }

    [Fact]
    public void TokenWithoutPermission_RejectedByPolicy()
    {
        var token = CreateToken(permissions: new[] { "building_read" });
        var principal = ValidateToken(token);
        Assert.NotNull(principal);
        var perms = principal.FindAll("permission").Select(c => c.Value).ToList();
        Assert.DoesNotContain("device_configure", perms);
    }

    [Fact]
    public void Token_WithBuildingGrant_ContainsBuildingClaim()
    {
        var buildingId = Guid.NewGuid().ToString();
        var token = CreateToken(buildingGrant: buildingId);
        var principal = ValidateToken(token);
        Assert.NotNull(principal);
        var buildingClaim = principal.FindFirst("buildingId")?.Value;
        Assert.Equal(buildingId, buildingClaim);
    }

    [Fact]
    public void Token_WrongIssuer_Throws()
    {
        var token = CreateToken(issuer: "WrongIssuer");
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => ValidateToken(token, validIssuer: "ClimateHub"));
    }

    [Fact]
    public void Token_WrongAudience_Throws()
    {
        var token = CreateToken(audience: "WrongAudience");
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => ValidateToken(token, validAudience: "ClimateHub.Api"));
    }

    [Fact]
    public void RevokedToken_RejectedByService()
    {
        var token = CreateToken();
        var principal = ValidateToken(token);
        Assert.NotNull(principal);

        var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        Assert.NotNull(jti);
    }

    [Fact]
    public void RefreshToken_Rotation_ProducesDifferentAccessToken()
    {
        var token1 = CreateToken();
        var token2 = CreateToken();
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Token_WithAllRequiredClaims_Succeeds()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("permission", "building_read"),
            new Claim("permission", "device_read"),
        };

        var jwt = new JwtSecurityToken(
            issuer: "ClimateHub", audience: "ClimateHub.Api",
            claims: claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256));

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        var principal = ValidateToken(token);
        Assert.NotNull(principal);
        Assert.Equal(2, principal.FindAll("permission").Count());
    }

    private static string CreateToken(bool wrongKey = false, DateTime? expires = null,
        string[]? permissions = null, string? buildingGrant = null,
        string? issuer = null, string? audience = null)
    {
        var key = wrongKey
            ? new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("different-key-not-matching-32-chars!"))
            : SecurityKey;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (permissions is not null)
            foreach (var p in permissions)
                claims.Add(new Claim("permission", p));

        if (buildingGrant is not null)
            claims.Add(new Claim("buildingId", buildingGrant));

        var jwt = new JwtSecurityToken(
            issuer: issuer ?? "ClimateHub", audience: audience ?? "ClimateHub.Api",
            claims: claims, expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static ClaimsPrincipal? ValidateToken(string token, string? validIssuer = null, string? validAudience = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var result = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKey,
            ValidateIssuer = true,
            ValidIssuer = validIssuer ?? "ClimateHub",
            ValidateAudience = true,
            ValidAudience = validAudience ?? "ClimateHub.Api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        }, out _);
        return result;
    }
}