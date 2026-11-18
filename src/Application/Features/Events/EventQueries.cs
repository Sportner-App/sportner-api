using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Organizations;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events;

internal static class EventQueries
{
    /// <summary>
    /// Etkinliğe bağlı içerik (gönderi, albüm) etkinliğin kendisinden daha görünür olmasın:
    /// organizasyona özel etkinlikte yalnızca onaylı üyeler görür.
    /// </summary>
    internal static async Task<bool> CanViewOrganizationScopeAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        var scope = await dbContext.Events.AsNoTracking()
            .Where(candidate => candidate.Id == eventId)
            .Select(candidate => new { candidate.OrganizationId, candidate.IsOrganizationOnly })
            .FirstOrDefaultAsync(cancellationToken);

        return scope?.OrganizationId is not { } orgId
            || !scope.IsOrganizationOnly
            || await OrganizationQueries.IsApprovedMemberAsync(dbContext, orgId, viewerUserId, cancellationToken);
    }

    /// <summary>
    /// Test hesabının düzenlediği ya da test hesabının kurduğu kulübün etkinliklerini, bakan
    /// test hesabı değilse listeden düşürür. Engel kuralları etkinliğe özgü olduğu için
    /// burada yalnızca test hesabı gizlemesi uygulanır.
    /// </summary>
    internal static IQueryable<Event> ExcludeTestHosted(
        IApplicationDbContext dbContext,
        IQueryable<Event> events,
        Guid? viewerUserId)
    {
        var hiddenIds = BlockQueries.HiddenTestAccountIds(dbContext, viewerUserId);

        return events.Where(@event =>
            !hiddenIds.Contains(@event.OrganizerUserId)
            && (@event.OrganizationId == null
                || !dbContext.Organizations.Any(organization =>
                    organization.Id == @event.OrganizationId
                    && hiddenIds.Contains(organization.FounderUserId))));
    }

    /// <summary>
    /// Etkinlik var, test hesabı içeriği ve bakan etkinliğe dahil değilse true. Dahil olanlar:
    /// iptal edilmemiş katılımcı kaydı olan, bekleme listesindeki ve etkinliği yönetebilen.
    /// </summary>
    internal static async Task<bool> IsHiddenTestHostedAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        var scope = await dbContext.Events.AsNoTracking()
            .Where(@event => @event.Id == eventId)
            .Select(@event => new { @event.OrganizerUserId, @event.OrganizationId })
            .FirstOrDefaultAsync(cancellationToken);

        if (scope is null
            || await ExcludeTestHosted(
                    dbContext,
                    dbContext.Events.AsNoTracking().Where(@event => @event.Id == eventId),
                    viewerUserId)
                .AnyAsync(cancellationToken))
        {
            return false;
        }

        if (viewerUserId is not { } viewer)
        {
            return true;
        }

        var involved = await dbContext.EventParticipants.AsNoTracking()
                .AnyAsync(
                    participant => participant.EventId == eventId
                        && participant.UserId == viewer
                        && participant.Status != ParticipantStatus.Cancelled,
                    cancellationToken)
            || await dbContext.EventWaitlists.AsNoTracking()
                .AnyAsync(entry => entry.EventId == eventId && entry.UserId == viewer, cancellationToken)
            || await EventAccess.CanManageAsync(
                dbContext, scope.OrganizerUserId, scope.OrganizationId, viewer, cancellationToken);

        return !involved;
    }

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

        if (await IsHiddenTestHostedAsync(dbContext, eventId, viewerUserId, cancellationToken))
        {
            return null;
        }

        // Taslak yalnızca yönetebilenlere (oluşturan / kulüp yöneticisi) ve taslağa eklenmiş
        // katılımcılara görünür (yayınlama başarısız olursa arkadaş davetleri taslağa düşer).
        if (@event.Status == EventStatus.Draft
            && !(viewerUserId is { } draftViewerId
                && await dbContext.EventParticipants.AsNoTracking().AnyAsync(
                    participant => participant.EventId == @event.Id
                        && participant.UserId == draftViewerId
                        && participant.Status != ParticipantStatus.Cancelled,
                    cancellationToken))
            && !await EventAccess.CanManageAsync(dbContext, @event, viewerUserId, cancellationToken))
        {
            return null;
        }

        if (@event.OrganizationId is { } organizationId
            && @event.IsOrganizationOnly
            && !await OrganizationQueries.IsApprovedMemberAsync(
                dbContext,
                organizationId,
                viewerUserId,
                cancellationToken))
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

        string? organizationName = null;
        string? organizationImageUrl = null;
        var isCancellableStatus = @event.Status is EventStatus.Draft or EventStatus.Published or EventStatus.Full;
        var canManage = viewerUserId is { } creatorCandidate && creatorCandidate == @event.OrganizerUserId;
        var canCancel = canManage && isCancellableStatus;

        if (@event.OrganizationId is { } orgId)
        {
            var organization = await dbContext.Organizations.AsNoTracking()
                .Where(candidate => candidate.Id == orgId)
                .Select(candidate => new { candidate.Name, candidate.ImageUrl })
                .FirstOrDefaultAsync(cancellationToken);
            organizationName = organization?.Name;
            organizationImageUrl = organization?.ImageUrl;

            if (!canManage && viewerUserId is { } managerId)
            {
                // Silinmiş kulüpte organization null döndüğü için yönetici yetkisi de düşer.
                canManage = organization is not null
                    && await EventAccess.IsClubManagerAsync(dbContext, orgId, managerId, cancellationToken);
                canCancel = canManage && isCancellableStatus;
            }
        }

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
            @event.ParticipantGender,
            @event.SkillLevel is { } skill ? (short)skill : null,
            @event.IsPaid,
            @event.FeeAmount,
            (short)@event.Status,
            occupied,
            waitlistCount,
            myStatus,
            isOnWaitlist,
            conversationId,
            @event.OrganizationId,
            organizationName,
            canCancel,
            @event.District,
            @event.City,
            @event.IsOrganizationOnly,
            organizationImageUrl,
            canManage,
            @event.IsOrganizerParticipant);
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
        join organization in dbContext.Organizations.AsNoTracking()
            on @event.OrganizationId equals organization.Id into organizations
        from organization in organizations.DefaultIfEmpty()
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
            @event.Latitude,
            @event.Longitude,
            @event.Address,
            @event.MaxParticipants,
            @event.MinParticipantAge,
            @event.MaxParticipantAge,
            @event.ParticipantGender,
            @event.SkillLevel != null ? (short?)@event.SkillLevel : null,
            @event.IsPaid,
            @event.FeeAmount,
            (short)@event.Status,
            dbContext.EventParticipants.Count(participant =>
                participant.EventId == @event.Id
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended
                    || participant.Status == ParticipantStatus.NoShow)),
            null,
            null,
            @event.OrganizationId,
            organization != null ? organization.Name : null,
            @event.IsOrganizationOnly,
            organization != null ? organization.ImageUrl : null,
            @event.IsOrganizerParticipant);
    }

    /// <summary>
    /// Max avatars returned for a list card's participant preview stack. Only the first 3 are
    /// drawn as distinct circles; a 4th is included so the "+N" overflow tile can use that
    /// participant's photo as its background instead of rendering empty.
    /// </summary>
    private const int MaxPreviewAvatars = 4;

    /// <summary>
    /// Fills in each item's <see cref="EventListItemResponse.ParticipantAvatars"/> for the small
    /// avatar stack on list cards. Done as a second pass (rather than a correlated subquery in
    /// <see cref="ProjectListItems"/>/callers) because "top N per event" isn't something EF Core
    /// translates reliably here — one query for all events on the page, grouped in memory, which is
    /// cheap since a page is only a handful of events.
    /// </summary>
    internal static async Task<IReadOnlyList<EventListItemResponse>> AttachParticipantAvatarsAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<EventListItemResponse> items,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var eventIds = items.Select(item => item.Id).ToList();
        var hiddenTestIds = BlockQueries.HiddenTestAccountIds(dbContext, viewerUserId);

        var rows = await (
                from participant in dbContext.EventParticipants.AsNoTracking()
                join profile in dbContext.UserProfiles.AsNoTracking()
                    on participant.UserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                where eventIds.Contains(participant.EventId)
                    && (participant.UserId == null || !hiddenTestIds.Contains(participant.UserId.Value))
                    && (participant.Status == ParticipantStatus.Approved
                        || participant.Status == ParticipantStatus.Attended
                        || participant.Status == ParticipantStatus.NoShow)
                orderby participant.CreatedAt
                select new
                {
                    participant.EventId,
                    participant.UserId,
                    IsGuest = participant.Kind == ParticipantKind.Guest,
                    Name = participant.Kind == ParticipantKind.Guest
                        ? participant.GuestFirstName
                        : profile != null ? profile.FirstName : null,
                    ProfileImageUrl = profile != null ? profile.ProfileImageUrl : null,
                })
            .ToListAsync(cancellationToken);

        var avatarsByEvent = rows
            .GroupBy(row => row.EventId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ParticipantAvatarResponse>)group
                    .Take(MaxPreviewAvatars)
                    .Select(row => new ParticipantAvatarResponse(
                        row.UserId,
                        row.Name,
                        row.ProfileImageUrl,
                        row.IsGuest))
                    .ToList());

        return items
            .Select(item => item with
            {
                ParticipantAvatars = avatarsByEvent.GetValueOrDefault(
                    item.Id,
                    Array.Empty<ParticipantAvatarResponse>()),
            })
            .ToList();
    }

    /// <summary>
    /// Fills in each item's <see cref="EventListItemResponse.MyParticipationStatus"/> so list
    /// cards can show a "you applied / you're in" badge, same as the event detail page already
    /// does. One query for the whole page, keyed by event id.
    /// </summary>
    internal static async Task<IReadOnlyList<EventListItemResponse>> AttachMyParticipationStatusAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<EventListItemResponse> items,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0 || viewerUserId is not { } viewerId)
        {
            return items;
        }

        var eventIds = items.Select(item => item.Id).ToList();

        var statusByEvent = await dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                eventIds.Contains(participant.EventId) && participant.UserId == viewerId)
            .Select(participant => new { participant.EventId, participant.Status })
            .ToDictionaryAsync(
                row => row.EventId,
                row => (short?)row.Status,
                cancellationToken);

        return items
            .Select(item => item with
            {
                MyParticipationStatus = statusByEvent.GetValueOrDefault(item.Id),
            })
            .ToList();
    }
}
