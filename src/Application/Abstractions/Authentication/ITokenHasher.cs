namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// Deterministic one-way hashing for secrets that must be stored only as hashes,
/// such as refresh tokens and OTP codes.
/// </summary>
public interface ITokenHasher
{
    string Hash(string value);

    bool Verify(string value, string hash);
}
