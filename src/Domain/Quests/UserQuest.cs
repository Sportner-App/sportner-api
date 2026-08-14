using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Quests;

public class UserQuest : AggregateRoot
{
    private UserQuest()
    {
    }

    public Guid UserId { get; private set; }

    public Guid QuestId { get; private set; }

    public QuestStatus Status { get; private set; }

    public int CurrentValue { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static UserQuest Start(Guid userId, Guid questId, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (questId == Guid.Empty)
        {
            throw new DomainException("Quest id is required.");
        }

        return new UserQuest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestId = questId,
            Status = QuestStatus.Active,
            CurrentValue = 0,
            CompletedAt = null,
            CreatedAt = utcNow
        };
    }

    /// <summary>
    /// Increments progress. Returns true when this call transitions the quest to Completed.
    /// </summary>
    public bool ReportProgress(int delta, int targetValue, DateTimeOffset utcNow)
    {
        if (delta <= 0)
        {
            throw new DomainException("Progress delta must be positive.");
        }

        if (targetValue <= 0)
        {
            throw new DomainException("Target value must be greater than zero.");
        }

        if (Status is not QuestStatus.Active)
        {
            return false;
        }

        var next = checked(CurrentValue + delta);
        CurrentValue = next;
        Touch(utcNow);

        if (CurrentValue < targetValue)
        {
            return false;
        }

        CurrentValue = targetValue;
        Status = QuestStatus.Completed;
        CompletedAt = utcNow;
        Touch(utcNow);
        return true;
    }

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;
}
