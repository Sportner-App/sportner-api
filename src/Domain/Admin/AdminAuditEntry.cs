using Sportner.Domain.Common.Base;

namespace Sportner.Domain.Admin;

/// <summary>Panelden yapılan her değişiklik: kim, ne yaptı, hangi kayda.</summary>
public class AdminAuditEntry : BaseEntity
{
    public const int MaxDetailsLength = 1000;

    private AdminAuditEntry()
    {
    }

    public Guid ActorUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string TargetType { get; private set; } = string.Empty;

    public Guid TargetId { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AdminAuditEntry Create(
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        string? details,
        DateTimeOffset utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details is { Length: > MaxDetailsLength } ? details[..MaxDetailsLength] : details,
            CreatedAt = utcNow,
        };
}
