using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.EventQuestions;

internal static class EventQuestionAccess
{
    internal static async Task<bool> CanWriteAsync(
        IApplicationDbContext dbContext,
        Event @event,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (@event.HasEnded(utcNow)
            || @event.Status is EventStatus.Draft or EventStatus.Cancelled)
        {
            return false;
        }

        if (@event.OrganizerUserId == userId)
        {
            return true;
        }

        return !await BlockQueries.BlockedPairExistsAsync(
            dbContext,
            userId,
            @event.OrganizerUserId,
            cancellationToken);
    }

    internal static async Task<HashSet<Guid>> ListParticipantUserIdsAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.EventId == eventId
                && participant.UserId != null
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended))
            .Select(participant => participant.UserId!.Value)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    internal static EventQnAAuthorRole ResolveRole(
        Guid authorUserId,
        Guid organizerUserId,
        IReadOnlySet<Guid> participantUserIds)
    {
        if (authorUserId == organizerUserId)
        {
            return EventQnAAuthorRole.Organizer;
        }

        return participantUserIds.Contains(authorUserId)
            ? EventQnAAuthorRole.Participant
            : EventQnAAuthorRole.Visitor;
    }

    internal static string Preview(string content) =>
        content.Length <= 120 ? content : content[..117] + "...";

    internal static async Task<EventQuestionResponse> ToResponseAsync(
        IApplicationDbContext dbContext,
        EventQuestion question,
        Guid organizerUserId,
        IReadOnlySet<Guid> participantUserIds,
        IReadOnlyList<EventQuestionResponse>? replies,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == question.AuthorUserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        string? replyToUsername = null;
        if (question.ReplyToUserId is { } replyToUserId)
        {
            replyToUsername = await dbContext.UserProfiles.AsNoTracking()
                .Where(candidate => candidate.UserId == replyToUserId)
                .Select(candidate => candidate.Username)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new EventQuestionResponse(
            question.Id,
            question.EventId,
            question.AuthorUserId,
            profile?.Username,
            profile?.FirstName,
            profile?.ProfileImageUrl,
            question.ParentId,
            question.ReplyToUserId,
            replyToUsername,
            question.Content,
            question.ReplyCount,
            (short)ResolveRole(question.AuthorUserId, organizerUserId, participantUserIds),
            question.CreatedAt,
            replies ?? []);
    }
}
