using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ClimateHub.EndToEndTests;

public static class TestAuthHelper
{
    private const string SigningKey = "test-signing-key-that-is-at-least-32-characters-long";

    private static readonly string[] DefaultPermissions =
    [
        "building_read", "building_configure",
        "device_read", "device_configure", "device_register", "device_delete",
        "environment_read",
        "policy_read", "policy_configure",
        "need_read", "need_execute", "need_configure",
        "engineering_read", "engineering_configure",
        "command_read", "command_create", "command_cancel"
    ];

    public static string GenerateToken(string userId = "11111111-1111-1111-1111-111111111111", string[]? permissions = null)
    {
        var key = ClimateHub.Modules.IAM.Domain.JwtSecurityKeys.Create(SigningKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "E2E Test Admin"),
            new("scope", "climate-hub-api")
        };

        permissions ??= DefaultPermissions;

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permission", perm));
        }

        var token = new JwtSecurityToken(
            issuer: "ClimateHub",
            audience: "ClimateHub.Api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
