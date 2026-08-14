using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Moderation;

internal static class ReportQueries
{
    internal static IQueryable<ReportResponse> Project(IApplicationDbContext dbContext) =>
        from report in dbContext.Reports.AsNoTracking()
        join reason in dbContext.ReportReasons.AsNoTracking()
            on report.ReportReasonId equals reason.Id into reasons
        from reason in reasons.DefaultIfEmpty()
        select new ReportResponse(
            report.Id,
            report.ReporterUserId,
            (short)report.EntityType,
            report.EntityId,
            report.ReportReasonId,
            reason != null ? reason.Code : null,
            reason != null ? reason.Name : null,
            report.Description,
            (short)report.Status,
            report.ReviewedByUserId,
            report.ReviewedAt,
            report.ResolutionNote,
            report.CreatedAt,
            report.UpdatedAt);

    internal static async Task<bool> TargetExistsAsync(
        IApplicationDbContext dbContext,
        ReportEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        entityType switch
        {
            ReportEntityType.User => await dbContext.Users.AsNoTracking()
                .AnyAsync(user => user.Id == entityId, cancellationToken),
            ReportEntityType.Event => await dbContext.Events.AsNoTracking()
                .AnyAsync(@event => @event.Id == entityId, cancellationToken),
            ReportEntityType.Post => await dbContext.Posts.AsNoTracking()
                .AnyAsync(post => post.Id == entityId, cancellationToken),
            ReportEntityType.Comment => await dbContext.PostComments.AsNoTracking()
                .AnyAsync(comment => comment.Id == entityId, cancellationToken),
            ReportEntityType.Review => await dbContext.Reviews.AsNoTracking()
                .AnyAsync(review => review.Id == entityId, cancellationToken),
            ReportEntityType.Message => await dbContext.Messages.AsNoTracking()
                .AnyAsync(message => message.Id == entityId, cancellationToken),
            ReportEntityType.Album => await dbContext.Albums.AsNoTracking()
                .AnyAsync(album => album.Id == entityId, cancellationToken),
            _ => false
        };

    internal static async Task<bool> IsOwnTargetAsync(
        IApplicationDbContext dbContext,
        Guid reporterUserId,
        ReportEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        entityType switch
        {
            ReportEntityType.User => reporterUserId == entityId,
            ReportEntityType.Event => await dbContext.Events.AsNoTracking()
                .AnyAsync(
                    @event => @event.Id == entityId && @event.OrganizerUserId == reporterUserId,
                    cancellationToken),
            ReportEntityType.Post => await dbContext.Posts.AsNoTracking()
                .AnyAsync(
                    post => post.Id == entityId && post.UserId == reporterUserId,
                    cancellationToken),
            ReportEntityType.Comment => await dbContext.PostComments.AsNoTracking()
                .AnyAsync(
                    comment => comment.Id == entityId && comment.UserId == reporterUserId,
                    cancellationToken),
            ReportEntityType.Review => await dbContext.Reviews.AsNoTracking()
                .AnyAsync(
                    review => review.Id == entityId && review.ReviewerUserId == reporterUserId,
                    cancellationToken),
            ReportEntityType.Message => await dbContext.Messages.AsNoTracking()
                .AnyAsync(
                    message => message.Id == entityId && message.SenderUserId == reporterUserId,
                    cancellationToken),
            ReportEntityType.Album => await IsOwnAlbumAsync(
                dbContext,
                reporterUserId,
                entityId,
                cancellationToken),
            _ => false
        };

    private static async Task<bool> IsOwnAlbumAsync(
        IApplicationDbContext dbContext,
        Guid reporterUserId,
        Guid albumId,
        CancellationToken cancellationToken)
    {
        var album = await dbContext.Albums.AsNoTracking()
            .Where(candidate => candidate.Id == albumId)
            .Select(candidate => new { candidate.Kind, candidate.OwnerUserId, candidate.EventId })
            .FirstOrDefaultAsync(cancellationToken);

        if (album is null)
        {
            return false;
        }

        if (album.Kind is AlbumKind.Profile)
        {
            return album.OwnerUserId == reporterUserId;
        }

        if (album.EventId is not { } eventId)
        {
            return false;
        }

        return await dbContext.Events.AsNoTracking()
            .AnyAsync(
                @event => @event.Id == eventId && @event.OrganizerUserId == reporterUserId,
                cancellationToken);
    }

    internal static async Task ApplyReviewSideEffectsAsync(
        IApplicationDbContext dbContext,
        Report report,
        bool markReported,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (report.EntityType is not ReportEntityType.Review)
        {
            return;
        }

        var review = await dbContext.Reviews
            .FirstOrDefaultAsync(candidate => candidate.Id == report.EntityId, cancellationToken);

        if (review is null)
        {
            return;
        }

        if (markReported)
        {
            review.MarkAsReported(utcNow);
        }
        else
        {
            review.ClearReportedStatus(utcNow);
        }
    }

    /// <summary>
    /// Resolve (<paramref name="hideOrFlag"/> = true) / Reject (false) side effects.
    /// User/Event: no auto Suspend/Cancel — Suspend is a separate Admin action.
    /// Message redact is one-way (reject does not restore content).
    /// </summary>
    internal static async Task ApplyTargetSideEffectsAsync(
        IApplicationDbContext dbContext,
        Report report,
        bool hideOrFlag,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        switch (report.EntityType)
        {
            case ReportEntityType.Review:
                await ApplyReviewSideEffectsAsync(
                    dbContext,
                    report,
                    markReported: hideOrFlag,
                    utcNow,
                    cancellationToken);
                break;

            case ReportEntityType.Post:
            {
                var post = await dbContext.Posts
                    .FirstOrDefaultAsync(candidate => candidate.Id == report.EntityId, cancellationToken);
                if (post is null)
                {
                    return;
                }

                if (hideOrFlag)
                {
                    post.Hide(utcNow);
                }
                else
                {
                    post.Unhide(utcNow);
                }

                break;
            }

            case ReportEntityType.Comment:
            {
                var comment = await dbContext.PostComments
                    .FirstOrDefaultAsync(candidate => candidate.Id == report.EntityId, cancellationToken);
                if (comment is null)
                {
                    return;
                }

                if (hideOrFlag)
                {
                    comment.Hide(utcNow);
                }
                else
                {
                    comment.Unhide(utcNow);
                }

                break;
            }

            case ReportEntityType.Message:
                if (!hideOrFlag)
                {
                    return;
                }

                var message = await dbContext.Messages
                    .FirstOrDefaultAsync(candidate => candidate.Id == report.EntityId, cancellationToken);
                message?.Redact(utcNow);
                break;

            case ReportEntityType.User:
            case ReportEntityType.Event:
                // No automatic Suspend / Cancel — separate admin / organizer commands.
                break;
        }
    }
}
