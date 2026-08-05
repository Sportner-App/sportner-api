using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Moderation;

public class Report : AggregateRoot
{
    private const int MaxDescriptionLength = 2000;
    private const int MaxResolutionNoteLength = 2000;

    private Report()
    {
    }

    public Guid ReporterUserId { get; private set; }

    public ReportEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public Guid ReportReasonId { get; private set; }

    public string? Description { get; private set; }

    public ReportStatus Status { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public string? ResolutionNote { get; private set; }

    public static Report Create(
        Guid reporterUserId,
        ReportEntityType entityType,
        Guid entityId,
        Guid reportReasonId,
        string? description,
        DateTimeOffset utcNow)
    {
        if (reporterUserId == Guid.Empty)
        {
            throw new DomainException("Reporter user id is required.");
        }

        if (entityId == Guid.Empty)
        {
            throw new DomainException("Entity id is required.");
        }

        if (reportReasonId == Guid.Empty)
        {
            throw new DomainException("Report reason id is required.");
        }

        if (!Enum.IsDefined(entityType))
        {
            throw new DomainException("Report entity type is invalid.");
        }

        return new Report
        {
            Id = Guid.NewGuid(),
            ReporterUserId = reporterUserId,
            EntityType = entityType,
            EntityId = entityId,
            ReportReasonId = reportReasonId,
            Description = NormalizeOptionalDescription(description),
            Status = ReportStatus.Pending,
            ReviewedByUserId = null,
            ReviewedAt = null,
            ResolutionNote = null,
            CreatedAt = utcNow
        };
    }

    public void StartReview(Guid moderatorUserId, DateTimeOffset utcNow)
    {
        EnsureModeratorUserId(moderatorUserId);

        if (Status is ReportStatus.UnderReview)
        {
            if (ReviewedByUserId == moderatorUserId)
            {
                return;
            }

            throw new DomainException("Report is already under review by another moderator.");
        }

        if (Status is ReportStatus.Resolved or ReportStatus.Rejected)
        {
            throw new DomainException("Closed reports cannot enter under review.");
        }

        if (Status is not ReportStatus.Pending)
        {
            throw new DomainException($"Report cannot enter under review from status '{Status}'.");
        }

        Status = ReportStatus.UnderReview;
        ReviewedByUserId = moderatorUserId;
        ReviewedAt = utcNow;
        ResolutionNote = null;
        Touch(utcNow);
    }

    public void Resolve(Guid moderatorUserId, string resolutionNote, DateTimeOffset utcNow)
    {
        EnsureModeratorUserId(moderatorUserId);
        var normalizedNote = NormalizeRequiredResolutionNote(resolutionNote);

        if (Status is ReportStatus.Resolved)
        {
            if (ReviewedByUserId == moderatorUserId
                && string.Equals(ResolutionNote, normalizedNote, StringComparison.Ordinal))
            {
                return;
            }

            throw new DomainException("Resolved reports cannot be resolved again with different data.");
        }

        if (Status is ReportStatus.Rejected)
        {
            throw new DomainException("Rejected reports cannot become resolved.");
        }

        EnsureCanClose(moderatorUserId);

        Status = ReportStatus.Resolved;
        ReviewedByUserId = moderatorUserId;
        ReviewedAt = utcNow;
        ResolutionNote = normalizedNote;
        Touch(utcNow);
    }

    public void Reject(Guid moderatorUserId, string resolutionNote, DateTimeOffset utcNow)
    {
        EnsureModeratorUserId(moderatorUserId);
        var normalizedNote = NormalizeRequiredResolutionNote(resolutionNote);

        if (Status is ReportStatus.Rejected)
        {
            if (ReviewedByUserId == moderatorUserId
                && string.Equals(ResolutionNote, normalizedNote, StringComparison.Ordinal))
            {
                return;
            }

            throw new DomainException("Rejected reports cannot be rejected again with different data.");
        }

        if (Status is ReportStatus.Resolved)
        {
            throw new DomainException("Resolved reports cannot become rejected.");
        }

        EnsureCanClose(moderatorUserId);

        Status = ReportStatus.Rejected;
        ReviewedByUserId = moderatorUserId;
        ReviewedAt = utcNow;
        ResolutionNote = normalizedNote;
        Touch(utcNow);
    }

    public void UpdateDescription(string? description, DateTimeOffset utcNow)
    {
        if (Status is not ReportStatus.Pending)
        {
            throw new DomainException("Description can only be updated while the report is pending.");
        }

        var normalized = NormalizeOptionalDescription(description);

        if (string.Equals(Description, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Description = normalized;
        Touch(utcNow);
    }

    public bool IsPending()
    {
        return Status is ReportStatus.Pending;
    }

    public bool IsUnderReview()
    {
        return Status is ReportStatus.UnderReview;
    }

    public bool IsClosed()
    {
        return Status is ReportStatus.Resolved or ReportStatus.Rejected;
    }

    public bool IsAbout(ReportEntityType entityType, Guid entityId)
    {
        if (!Enum.IsDefined(entityType))
        {
            throw new DomainException("Report entity type is invalid.");
        }

        if (entityId == Guid.Empty)
        {
            throw new DomainException("Entity id is required.");
        }

        return EntityType == entityType && EntityId == entityId;
    }

    public bool WasReportedBy(Guid userId)
    {
        return ReporterUserId == userId;
    }

    private void EnsureCanClose(Guid moderatorUserId)
    {
        if (Status is ReportStatus.UnderReview
            && ReviewedByUserId != moderatorUserId)
        {
            throw new DomainException("Only the assigned moderator can close this report.");
        }

        if (Status is not (ReportStatus.Pending or ReportStatus.UnderReview))
        {
            throw new DomainException($"Report cannot be closed from status '{Status}'.");
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static void EnsureModeratorUserId(Guid moderatorUserId)
    {
        if (moderatorUserId == Guid.Empty)
        {
            throw new DomainException("Moderator user id is required.");
        }
    }

    private static string? NormalizeOptionalDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();

        if (normalized.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Description cannot exceed {MaxDescriptionLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeRequiredResolutionNote(string resolutionNote)
    {
        if (string.IsNullOrWhiteSpace(resolutionNote))
        {
            throw new DomainException("Resolution note is required.");
        }

        var normalized = resolutionNote.Trim();

        if (normalized.Length > MaxResolutionNoteLength)
        {
            throw new DomainException($"Resolution note cannot exceed {MaxResolutionNoteLength} characters.");
        }

        return normalized;
    }
}
