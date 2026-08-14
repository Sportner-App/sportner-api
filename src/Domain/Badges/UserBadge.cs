using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Badges;

public class UserBadge : AggregateRoot
{
    public const short MaxShowcaseSlots = 3;

    private UserBadge()
    {
    }

    public Guid UserId { get; private set; }

    public Guid BadgeId { get; private set; }

    public DateTimeOffset EarnedAt { get; private set; }

    public bool IsShowcased { get; private set; }

    /// <summary>1-based order among showcased badges; null when not showcased.</summary>
    public short? ShowcaseOrder { get; private set; }

    public static UserBadge Award(
        Guid userId,
        Guid badgeId,
        DateTimeOffset earnedAt,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (badgeId == Guid.Empty)
        {
            throw new DomainException("Badge id is required.");
        }

        if (earnedAt > utcNow)
        {
            throw new DomainException("Earned at cannot be later than the current time.");
        }

        return new UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BadgeId = badgeId,
            EarnedAt = earnedAt,
            IsShowcased = false,
            ShowcaseOrder = null,
            CreatedAt = utcNow
        };
    }

    public void SetShowcased(short order, DateTimeOffset utcNow)
    {
        if (order is < 1 or > MaxShowcaseSlots)
        {
            throw new DomainException($"Showcase order must be between 1 and {MaxShowcaseSlots}.");
        }

        if (IsShowcased && ShowcaseOrder == order)
        {
            return;
        }

        IsShowcased = true;
        ShowcaseOrder = order;
        Touch(utcNow);
    }

    public void ClearShowcase(DateTimeOffset utcNow)
    {
        if (!IsShowcased && ShowcaseOrder is null)
        {
            return;
        }

        IsShowcased = false;
        ShowcaseOrder = null;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }
}
