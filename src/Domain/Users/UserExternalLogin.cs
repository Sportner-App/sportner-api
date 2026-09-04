using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Users;

public class UserExternalLogin : AuditableEntity
{
    private UserExternalLogin()
    {
    }

    public Guid UserId { get; private set; }

    public ExternalLoginProvider Provider { get; private set; }

    /// <summary>The provider's stable subject identifier (Google/Apple "sub" claim).</summary>
    public string ProviderUserId { get; private set; } = null!;

    /// <summary>Apple only sends this on the first authorization; may be null afterwards.</summary>
    public string? Email { get; private set; }

    public static UserExternalLogin Create(
        Guid userId,
        ExternalLoginProvider provider,
        string providerUserId,
        string? email,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        EnsureDefinedProvider(provider);

        return new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderUserId = NormalizeProviderUserId(providerUserId),
            Email = NormalizeOptionalEmail(email),
            CreatedAt = utcNow
        };
    }

    private static void EnsureDefinedProvider(ExternalLoginProvider provider)
    {
        if (!Enum.IsDefined(provider))
        {
            throw new DomainException("External login provider is invalid.");
        }
    }

    private static string NormalizeProviderUserId(string providerUserId)
    {
        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            throw new DomainException("Provider user id is required.");
        }

        var normalized = providerUserId.Trim();

        if (normalized.Length > 255)
        {
            throw new DomainException("Provider user id cannot exceed 255 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim();

        if (normalized.Length > 255)
        {
            throw new DomainException("Email cannot exceed 255 characters.");
        }

        return normalized;
    }
}
