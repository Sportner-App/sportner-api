using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Challenges;

public class ChallengeParticipant : AggregateRoot
{
    private ChallengeParticipant()
    {
    }

    public Guid ChallengeId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static ChallengeParticipant Join(Guid challengeId, Guid userId, DateTimeOffset utcNow)
    {
        if (challengeId == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainException("Challenge and user are required.");
        }

        return new ChallengeParticipant
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            UserId = userId,
            JoinedAt = utcNow,
            CreatedAt = utcNow
        };
    }

    /// <returns>İlk kez tamamlandıysa true.</returns>
    public bool MarkCompleted(DateTimeOffset utcNow)
    {
        if (CompletedAt is not null)
        {
            return false;
        }

        CompletedAt = utcNow;
        UpdatedAt = utcNow;
        return true;
    }
}
