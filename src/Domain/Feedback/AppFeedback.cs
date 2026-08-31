using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Feedback;

public class AppFeedback : AggregateRoot
{
    public const int MinContentLength = 10;
    public const int MaxContentLength = 2000;

    private AppFeedback()
    {
    }

    public Guid UserId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public static AppFeedback Create(
        Guid userId,
        string content,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        return new AppFeedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = NormalizeContent(content),
            CreatedAt = utcNow
        };
    }

    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Feedback content is required.");
        }

        var normalized = content.Trim();

        if (normalized.Length < MinContentLength)
        {
            throw new DomainException(
                $"Feedback content must be at least {MinContentLength} characters.");
        }

        if (normalized.Length > MaxContentLength)
        {
            throw new DomainException(
                $"Feedback content cannot exceed {MaxContentLength} characters.");
        }

        return normalized;
    }
}
