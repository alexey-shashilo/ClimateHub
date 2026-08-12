using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ClimateHub.Modules.IAM.Domain;

/// <summary>
/// Creates <see cref="SymmetricSecurityKey"/> instances with an explicit, deterministic
/// <c>kid</c>. JWT bearer validation in IdentityModel 8.x requires the token's header
/// <c>kid</c> to match the configured validation key. Without it, validation fails with
/// IDX10517 ("signature key was not found") even when the secret bytes are identical.
/// </summary>
public static class JwtSecurityKeys
{
    /// <summary>
    /// Builds a symmetric key for the given secret with a stable KeyId derived from the
    /// secret's SHA-256 digest so signing and validation always resolve to the same key.
    /// </summary>
    public static SymmetricSecurityKey Create(string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var key = new SymmetricSecurityKey(secretBytes);
        key.KeyId = ComputeKeyId(secretBytes);
        return key;
    }

    private static string ComputeKeyId(byte[] secretBytes)
    {
        var hash = SHA256.HashData(secretBytes);
        return Convert.ToHexString(hash);
    }
}
