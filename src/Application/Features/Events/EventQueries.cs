using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;

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

        var sport = await dbContext.Sports.AsNoTracking()
            .Where(candidate => candidate.Id == @event.SportId)
            .Select(candidate => new { candidate.Name, candidate.Slug })
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
                    && (participant.Status == ParticipantStatus.Pending
                        || participant.Status == ParticipantStatus.Approved
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
            organizer,
            @event.Title,
            @event.Description,
            @event.EventDate,
            @event.DurationMinutes,
            @event.Latitude,
            @event.Longitude,
            @event.Address,
            @event.MaxParticipants,
            (short)@event.Status,
            occupied,
            waitlistCount,
            myStatus,
            isOnWaitlist,
            conversationId);
    }

    internal static IQueryable<EventListItemResponse> ProjectListItems(
        IApplicationDbContext dbContext) =>
        from @event in dbContext.Events.AsNoTracking()
        join sport in dbContext.Sports.AsNoTracking() on @event.SportId equals sport.Id
        join profile in dbContext.UserProfiles.AsNoTracking()
            on @event.OrganizerUserId equals profile.UserId into profiles
        from profile in profiles.DefaultIfEmpty()
        select new EventListItemResponse(
            @event.Id,
            @event.SportId,
            sport.Name,
            sport.Slug,
            @event.OrganizerUserId,
            profile != null ? profile.Username : null,
            @event.Title,
            @event.EventDate,
            @event.DurationMinutes,
            @event.Address,
            @event.MaxParticipants,
            (short)@event.Status,
            dbContext.EventParticipants.Count(participant =>
                participant.EventId == @event.Id
                && (participant.Status == ParticipantStatus.Pending
                    || participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended
                    || participant.Status == ParticipantStatus.NoShow)));
}
