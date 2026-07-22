using System.Security.Cryptography;
using System.Text;

namespace DailyMart.Application.Auth;

/// <summary>
/// Refresh tokens are opaque random strings, not JWTs - only their SHA-256 hash is ever persisted
/// (see RefreshToken.TokenHash), so a database leak alone can't be exchanged for a working session.
/// </summary>
internal static class RefreshTokenHasher
{
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
