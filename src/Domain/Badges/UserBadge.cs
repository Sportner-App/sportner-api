using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Badges;

public class UserBadge : AggregateRoot
{
    private UserBadge()
    {
    }

    public Guid UserId { get; private set; }

    public Guid BadgeId { get; private set; }

    public DateTimeOffset EarnedAt { get; private set; }

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
            CreatedAt = utcNow
        };
    }
}
