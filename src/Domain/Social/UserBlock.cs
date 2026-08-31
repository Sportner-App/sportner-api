using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class UserBlock : AggregateRoot
{
    private UserBlock()
    {
    }

    public Guid BlockerUserId { get; private set; }

    public Guid BlockedUserId { get; private set; }

    public static UserBlock Create(
        Guid blockerUserId,
        Guid blockedUserId,
        DateTimeOffset utcNow)
    {
        if (blockerUserId == Guid.Empty)
        {
            throw new DomainException("Blocker user id is required.");
        }

        if (blockedUserId == Guid.Empty)
        {
            throw new DomainException("Blocked user id is required.");
        }

        if (blockerUserId == blockedUserId)
        {
            throw new DomainException("Users cannot block themselves.");
        }

        return new UserBlock
        {
            Id = Guid.NewGuid(),
            BlockerUserId = blockerUserId,
            BlockedUserId = blockedUserId,
            CreatedAt = utcNow
        };
    }
}
