using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class Friendship : AggregateRoot
{
    private Friendship()
    {
    }

    public Guid RequesterUserId { get; private set; }

    public Guid AddresseeUserId { get; private set; }

    public FriendshipStatus Status { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public static Friendship CreateRequest(
        Guid requesterUserId,
        Guid addresseeUserId,
        DateTimeOffset utcNow)
    {
        if (requesterUserId == Guid.Empty)
        {
            throw new DomainException("Requester user id is required.");
        }

        if (addresseeUserId == Guid.Empty)
        {
            throw new DomainException("Addressee user id is required.");
        }

        if (requesterUserId == addresseeUserId)
        {
            throw new DomainException("Users cannot send a friend request to themselves.");
        }

        return new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterUserId = requesterUserId,
            AddresseeUserId = addresseeUserId,
            Status = FriendshipStatus.Pending,
            RespondedAt = null,
            CreatedAt = utcNow
        };
    }

    public void Accept(DateTimeOffset utcNow)
    {
        if (Status is FriendshipStatus.Accepted)
        {
            return;
        }

        if (Status is not FriendshipStatus.Pending)
        {
            throw new DomainException($"Friendship cannot be accepted from status '{Status}'.");
        }

        Status = FriendshipStatus.Accepted;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public void Reject(DateTimeOffset utcNow)
    {
        if (Status is FriendshipStatus.Rejected)
        {
            return;
        }

        if (Status is not FriendshipStatus.Pending)
        {
            throw new DomainException($"Friendship cannot be rejected from status '{Status}'.");
        }

        Status = FriendshipStatus.Rejected;
        RespondedAt = utcNow;
        Touch(utcNow);
    }

    public bool IsPending()
    {
        return Status is FriendshipStatus.Pending;
    }

    public bool IsAccepted()
    {
        return Status is FriendshipStatus.Accepted;
    }

    public bool InvolvesUser(Guid userId)
    {
        return RequesterUserId == userId || AddresseeUserId == userId;
    }

    public bool IsBetween(Guid firstUserId, Guid secondUserId)
    {
        return (RequesterUserId == firstUserId && AddresseeUserId == secondUserId)
            || (RequesterUserId == secondUserId && AddresseeUserId == firstUserId);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }
}
