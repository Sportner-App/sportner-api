using Microsoft.AspNetCore.Identity;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Wraps ASP.NET Identity <see cref="PasswordHasher{TUser}"/> (PBKDF2).
/// </summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(new object(), password);

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(new object(), passwordHash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
