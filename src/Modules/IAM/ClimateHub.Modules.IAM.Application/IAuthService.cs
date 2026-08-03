using ClimateHub.Modules.IAM.Contracts;

namespace ClimateHub.Modules.IAM.Domain;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<RefreshResponse> RefreshTokenAsync(RefreshRequest request, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, Guid userId, CancellationToken ct = default);
    Task<MeResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}