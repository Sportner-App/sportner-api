namespace Sportner.Domain.Common.Base;

public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public Guid? CreatedByUserId { get; protected set; }

    public Guid? UpdatedByUserId { get; protected set; }
}
