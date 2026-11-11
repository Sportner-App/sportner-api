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

    public const int MaxReplyLength = 4000;

    public AppFeedbackStatus Status { get; private set; }

    public string? AdminReply { get; private set; }

    public DateTimeOffset? RepliedAt { get; private set; }

    public Guid? RepliedByUserId { get; private set; }

    public void SetStatus(AppFeedbackStatus status, DateTimeOffset utcNow)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedAt = utcNow;
    }

    /// <summary>Yanıt maille gönderildikten sonra kaydedilir; geri bildirim çözüldü sayılır.</summary>
    public void RecordReply(Guid adminUserId, string reply, DateTimeOffset utcNow)
    {
        var normalized = reply?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > MaxReplyLength)
        {
            throw new DomainException("Reply must be between 1 and 4000 characters.");
        }

        AdminReply = normalized;
        RepliedAt = utcNow;
        RepliedByUserId = adminUserId;
        Status = AppFeedbackStatus.Resolved;
        UpdatedAt = utcNow;
    }

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
