using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Admin;

/// <summary>
/// Yönetici paneline e-postayla gelen tek kullanımlık giriş kodu. Kodun kendisi değil
/// özeti saklanır; birkaç yanlış denemeden sonra kod geçersiz olur.
/// </summary>
public class AdminLoginCode : AuditableEntity
{
    public const int MaxAttempts = 5;

    private AdminLoginCode()
    {
    }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public static AdminLoginCode Create(
        Guid userId,
        string codeHash,
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(codeHash))
        {
            throw new DomainException("User and code are required.");
        }

        return new AdminLoginCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            CreatedAt = utcNow,
        };
    }

    public bool IsUsable(DateTimeOffset utcNow) =>
        ConsumedAt is null && FailedAttempts < MaxAttempts && utcNow < ExpiresAt;

    public void RegisterFailure(DateTimeOffset utcNow)
    {
        FailedAttempts++;
        UpdatedAt = utcNow;
    }

    public void Consume(DateTimeOffset utcNow)
    {
        ConsumedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
