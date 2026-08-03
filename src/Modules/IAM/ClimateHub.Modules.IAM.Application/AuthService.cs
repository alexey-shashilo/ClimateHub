using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ClimateHub.Modules.IAM.Contracts;
using ClimateHub.Modules.IAM.Domain;

namespace ClimateHub.Modules.IAM.Application;

public enum PasswordHashResult
{
    Failed,
    Success
}

public interface IPasswordHasherService
{
    string HashPassword(string password);
    PasswordHashResult VerifyHashedPassword(string hashedPassword, string providedPassword);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IBuildingAccessGrantRepository _buildingAccessGrantRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly JwtOptions _jwtOptions;
    private readonly IPasswordHasherService _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IBuildingAccessGrantRepository buildingAccessGrantRepository,
        IRefreshSessionRepository refreshSessionRepository,
        IOptions<JwtOptions> jwtOptions,
        IPasswordHasherService passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _buildingAccessGrantRepository = buildingAccessGrantRepository;
        _refreshSessionRepository = refreshSessionRepository;
        _jwtOptions = jwtOptions.Value;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user.PasswordHash, request.Password);
        if (verificationResult is PasswordHashResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials");

        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        var roles = await _userRepository.GetUserRolesAsync(user.Id, ct);
        var permissions = roles.SelectMany(r => r.Permissions).Distinct().ToList();

        var accessToken = GenerateAccessToken(user, roles, permissions);
        var (rawToken, tokenHash) = GenerateRefreshToken();
        var familyId = Guid.NewGuid();

        var refreshSession = new RefreshSession(
            Guid.NewGuid(),
            user.Id,
            tokenHash,
            familyId,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            createdByIp: request.IpAddress);

        await _refreshSessionRepository.AddAsync(refreshSession, ct);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            User = new UserInfo
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                DisplayName = user.DisplayName,
                Permissions = permissions.Select(p => p.ToString()).ToList(),
                Roles = roles.Select(r => r.Name).ToList()
            }
        };
    }

    public async Task<RefreshResponse> RefreshTokenAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var tokenHash = RefreshSession.HashToken(request.RefreshToken);
        var session = await _refreshSessionRepository.GetByTokenHashAsync(tokenHash, ct);

        if (session is null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        // Reuse detection: if token was already consumed or revoked, revoke entire family
        if (!session.IsActive)
        {
            await _refreshSessionRepository.RevokeFamilyAsync(session.FamilyId, "TOKEN_REUSE_DETECTED", ct);
            throw new UnauthorizedAccessException("Refresh token reuse detected");
        }

        var user = await _userRepository.GetByIdAsync(session.UserId, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        var roles = await _userRepository.GetUserRolesAsync(user.Id, ct);
        var permissions = roles.SelectMany(r => r.Permissions).Distinct().ToList();

        var newAccessToken = GenerateAccessToken(user, roles, permissions);
        var (newRawToken, newTokenHash) = GenerateRefreshToken();

        // Rotate within same family
        var newSession = new RefreshSession(
            Guid.NewGuid(),
            user.Id,
            newTokenHash,
            session.FamilyId,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            parentSessionId: session.Id);

        // Mark old as consumed, linking to new session
        session.MarkConsumed(newSession.Id);
        await _refreshSessionRepository.UpdateAsync(session, ct);
        await _refreshSessionRepository.AddAsync(newSession, ct);

        return new RefreshResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken, Guid userId, CancellationToken ct = default)
    {
        var tokenHash = RefreshSession.HashToken(refreshToken);
        var session = await _refreshSessionRepository.GetByTokenHashAsync(tokenHash, ct);
        if (session is null || session.UserId != userId)
            return;

        session.Revoke("USER_REVOKED");
        await _refreshSessionRepository.UpdateAsync(session, ct);
    }

    public async Task<MeResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new KeyNotFoundException("User not found");

        var roles = await _userRepository.GetUserRolesAsync(user.Id, ct);
        var permissions = roles.SelectMany(r => r.Permissions).Distinct().ToList();

        return new MeResponse
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Permissions = permissions.Select(p => p.ToString()).ToList(),
            Roles = roles.Select(r => r.Name).ToList()
        };
    }

    private string GenerateAccessToken(UserAccount user, List<Role> roles, List<Permission> permissions)
    {
        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));

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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static (string rawToken, string tokenHash) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var raw = Convert.ToBase64String(randomBytes);
        return (raw, RefreshSession.HashToken(raw));
    }
}