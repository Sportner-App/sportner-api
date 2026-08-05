using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Reviews;

public class Review : AggregateRoot
{
    private const int MaxCommentLength = 1000;

    private Review()
    {
    }

    public Guid EventId { get; private set; }

    public Guid ReviewerUserId { get; private set; }

    public Guid ReviewedUserId { get; private set; }

    public short Rating { get; private set; }

    public string? Comment { get; private set; }

    public bool IsReported { get; private set; }

    public static Review Create(
        Guid eventId,
        Guid reviewerUserId,
        Guid reviewedUserId,
        short rating,
        string? comment,
        DateTimeOffset utcNow)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        if (reviewerUserId == Guid.Empty)
        {
            throw new DomainException("Reviewer user id is required.");
        }

        if (reviewedUserId == Guid.Empty)
        {
            throw new DomainException("Reviewed user id is required.");
        }

        if (reviewerUserId == reviewedUserId)
        {
            throw new DomainException("Users cannot review themselves.");
        }

        return new Review
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ReviewerUserId = reviewerUserId,
            ReviewedUserId = reviewedUserId,
            Rating = NormalizeRating(rating),
            Comment = NormalizeComment(comment),
            IsReported = false,
            CreatedAt = utcNow
        };
    }

    public void UpdateRating(short rating, DateTimeOffset utcNow)
    {
        var normalized = NormalizeRating(rating);

        if (Rating == normalized)
        {
            return;
        }

        Rating = normalized;
        Touch(utcNow);
    }

    public void UpdateComment(string? comment, DateTimeOffset utcNow)
    {
        var normalized = NormalizeComment(comment);

        if (string.Equals(Comment, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Comment = normalized;
        Touch(utcNow);
    }

    public void Update(short rating, string? comment, DateTimeOffset utcNow)
    {
        var normalizedRating = NormalizeRating(rating);
        var normalizedComment = NormalizeComment(comment);

        var ratingChanged = Rating != normalizedRating;
        var commentChanged = !string.Equals(Comment, normalizedComment, StringComparison.Ordinal);

        if (!ratingChanged && !commentChanged)
        {
            return;
        }

        Rating = normalizedRating;
        Comment = normalizedComment;
        Touch(utcNow);
    }

    public void MarkAsReported(DateTimeOffset utcNow)
    {
        if (IsReported)
        {
            return;
        }

        IsReported = true;
        Touch(utcNow);
    }

    public void ClearReportedStatus(DateTimeOffset utcNow)
    {
        if (!IsReported)
        {
            return;
        }

        IsReported = false;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static short NormalizeRating(short rating)
    {
        if (rating is < 1 or > 5)
        {
            throw new DomainException("Rating must be between 1 and 5.");
        }

        return rating;
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var normalized = comment.Trim();

        if (normalized.Length > MaxCommentLength)
        {
            throw new DomainException($"Comment cannot exceed {MaxCommentLength} characters.");
        }

        return normalized;
    }
}
