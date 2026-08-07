using System.Security.Cryptography;
using System.Text;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// SHA-256 hashing for opaque secrets (refresh tokens, OTP codes) stored only as hashes.
/// Comparison is constant-time.
/// </summary>
public sealed class TokenHasher : ITokenHasher
{
    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    public bool Verify(string value, string hash)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var computed = Hash(value);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }
}
