using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events;

internal static class EventQueries
{
    internal static async Task<EventResponse?> GetDetailAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        var @event = await dbContext.Events.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == eventId, cancellationToken);

        if (@event is null)
        {
            return null;
        }

        if (viewerUserId is { } viewer
            && viewer != @event.OrganizerUserId
            && await BlockQueries.BlockedPairExistsAsync(
                dbContext,
                viewer,
                @event.OrganizerUserId,
                cancellationToken))
        {
            var involved = await dbContext.EventParticipants.AsNoTracking()
                .AnyAsync(
                    participant =>
                        participant.EventId == eventId && participant.UserId == viewer,
                    cancellationToken)
                || await dbContext.EventWaitlists.AsNoTracking()
                    .AnyAsync(
                        entry => entry.EventId == eventId && entry.UserId == viewer,
                        cancellationToken);

            if (!involved)
            {
                return null;
            }
        }

        var sport = await dbContext.Sports.AsNoTracking()
            .Where(candidate => candidate.Id == @event.SportId)
            .Select(candidate => new { candidate.Name, candidate.Slug, candidate.CoverImageUrl })
            .FirstAsync(cancellationToken);

        var organizer = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == @event.OrganizerUserId)
            .Select(profile => new OrganizerSnippetResponse(
                profile.UserId,
                profile.Username,
                profile.FirstName,
                profile.LastName,
                profile.ProfileImageUrl))
            .FirstOrDefaultAsync(cancellationToken)
            ?? new OrganizerSnippetResponse(@event.OrganizerUserId, null, null, null, null);

        var occupied = await dbContext.EventParticipants.AsNoTracking()
            .CountAsync(
                participant =>
                    participant.EventId == eventId
                    && (participant.Status == ParticipantStatus.Approved
                        || participant.Status == ParticipantStatus.Attended
                        || participant.Status == ParticipantStatus.NoShow),
                cancellationToken);

        var waitlistCount = await dbContext.EventWaitlists.AsNoTracking()
            .CountAsync(entry => entry.EventId == eventId, cancellationToken);

        short? myStatus = null;
        var isOnWaitlist = false;

        if (viewerUserId is not null)
        {
            myStatus = await dbContext.EventParticipants.AsNoTracking()
                .Where(participant =>
                    participant.EventId == eventId && participant.UserId == viewerUserId)
                .Select(participant => (short?)participant.Status)
                .FirstOrDefaultAsync(cancellationToken);

            isOnWaitlist = await dbContext.EventWaitlists.AsNoTracking()
                .AnyAsync(
                    entry => entry.EventId == eventId && entry.UserId == viewerUserId.Value,
                    cancellationToken);
        }

        var conversationId = await dbContext.Conversations.AsNoTracking()
            .Where(conversation =>
                conversation.EventId == eventId && conversation.Type == ConversationType.Event)
            .Select(conversation => (Guid?)conversation.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new EventResponse(
            @event.Id,
            @event.SportId,
            sport.Name,
            sport.Slug,
            sport.CoverImageUrl,
            organizer,
            @event.Title,
            @event.Description,
            @event.EventDate,
            @event.DurationMinutes,
            @event.Latitude,
            @event.Longitude,
            @event.Address,
            @event.MaxParticipants,
            @event.MinParticipantAge,
            @event.MaxParticipantAge,
            @event.SkillLevel is { } skill ? (short)skill : null,
            @event.IsPaid,
            @event.FeeAmount,
            (short)@event.Status,
            occupied,
            waitlistCount,
            myStatus,
            isOnWaitlist,
            conversationId);
    }

    /// <summary>
    /// Filters must be applied on <paramref name="events"/> (entity), not on the
    /// projected DTO — EF cannot translate Where/Contains after the occupied Count.
    /// </summary>
    internal static IQueryable<EventListItemResponse> ProjectListItems(
        IApplicationDbContext dbContext,
        IQueryable<Event>? events = null)
    {
        var source = events ?? dbContext.Events.AsNoTracking();

        return from @event in source
        join sport in dbContext.Sports.AsNoTracking() on @event.SportId equals sport.Id
        join profile in dbContext.UserProfiles.AsNoTracking()
            on @event.OrganizerUserId equals profile.UserId into profiles
        from profile in profiles.DefaultIfEmpty()
        select new EventListItemResponse(
            @event.Id,
            @event.SportId,
            sport.Name,
            sport.Slug,
            sport.CoverImageUrl,
            @event.OrganizerUserId,
            profile != null ? profile.Username : null,
            @event.Title,
            @event.EventDate,
            @event.DurationMinutes,
            @event.Address,
            @event.MaxParticipants,
            @event.MinParticipantAge,
            @event.MaxParticipantAge,
            @event.SkillLevel != null ? (short?)@event.SkillLevel : null,
            @event.IsPaid,
            @event.FeeAmount,
            (short)@event.Status,
            dbContext.EventParticipants.Count(participant =>
                participant.EventId == @event.Id
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended
                    || participant.Status == ParticipantStatus.NoShow)));
    }
}
