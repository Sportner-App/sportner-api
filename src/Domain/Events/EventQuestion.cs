using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

public class EventQuestion : AggregateRoot
{
    public const int MinContentLength = 5;
    public const int MaxContentLength = 1000;

    private EventQuestion()
    {
    }

    public Guid EventId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public Guid? ParentId { get; private set; }

    public Guid? ReplyToUserId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public int ReplyCount { get; private set; }

    public static EventQuestion CreateQuestion(
        Guid eventId,
        Guid authorUserId,
        string content,
        DateTimeOffset utcNow)
    {
        return Create(eventId, authorUserId, content, parentId: null, replyToUserId: null, utcNow);
    }

    public static EventQuestion CreateReply(
        Guid eventId,
        Guid authorUserId,
        Guid parentId,
        string content,
        DateTimeOffset utcNow,
        Guid? replyToUserId = null)
    {
        if (parentId == Guid.Empty)
        {
            throw new DomainException("Parent question id is required.");
        }

        return Create(
            eventId,
            authorUserId,
            content,
            parentId,
            replyToUserId == Guid.Empty ? null : replyToUserId,
            utcNow);
    }

    public bool IsReply() => ParentId is not null;

    public void IncrementReplyCount(DateTimeOffset utcNow, int amount = 1)
    {
        if (IsReply())
        {
            throw new DomainException("Only root questions track reply count.");
        }

        if (amount < 1)
        {
            throw new DomainException("Reply count increment must be positive.");
        }

        ReplyCount += amount;
        Touch(utcNow);
    }

    private static EventQuestion Create(
        Guid eventId,
        Guid authorUserId,
        string content,
        Guid? parentId,
        Guid? replyToUserId,
        DateTimeOffset utcNow)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainException("Author user id is required.");
        }

        return new EventQuestion
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AuthorUserId = authorUserId,
            ParentId = parentId,
            ReplyToUserId = replyToUserId,
            Content = NormalizeContent(content),
            ReplyCount = 0,
            CreatedAt = utcNow
        };
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Question content is required.");
        }

        var normalized = content.Trim();

        if (normalized.Length < MinContentLength)
        {
            throw new DomainException(
                $"Question content must be at least {MinContentLength} characters.");
        }

        if (normalized.Length > MaxContentLength)
        {
            throw new DomainException(
                $"Question content cannot exceed {MaxContentLength} characters.");
        }

        return normalized;
    }
}
