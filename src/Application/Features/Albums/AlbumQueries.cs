using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Albums;

internal static class AlbumQueries
{
    internal static AlbumResponse ToResponse(Album album) =>
        new(
            album.Id,
            (short)album.Kind,
            album.OwnerUserId,
            album.EventId,
            album.Title,
            album.Description,
            (short)album.Visibility,
            album.CoverMediaId,
            album.MediaCount,
            album.CreatedAt,
            album.Media
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new AlbumMediaResponse(
                    item.Id,
                    item.StoragePath,
                    item.FileName,
                    item.MimeType,
                    item.FileSize,
                    item.Width,
                    item.Height,
                    item.DisplayOrder,
                    item.UploadedByUserId,
                    item.CreatedAt))
                .ToList());

    internal static AlbumListItemResponse ToListItem(Album album) =>
        new(
            album.Id,
            (short)album.Kind,
            album.OwnerUserId,
            album.EventId,
            album.Title,
            album.Description,
            (short)album.Visibility,
            album.CoverMediaId,
            album.MediaCount,
            album.CreatedAt);

    internal static async Task<bool> CanManageAsync(
        IApplicationDbContext dbContext,
        Album album,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (album.Kind is AlbumKind.Profile)
        {
            return album.OwnerUserId == userId;
        }

        if (album.EventId is not { } eventId)
        {
            return false;
        }

        return await dbContext.Events.AsNoTracking()
            .AnyAsync(
                @event => @event.Id == eventId && @event.OrganizerUserId == userId,
                cancellationToken);
    }

    internal static async Task<bool> CanUploadMediaAsync(
        IApplicationDbContext dbContext,
        Album album,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (album.Kind is AlbumKind.Profile)
        {
            return album.OwnerUserId == userId;
        }

        if (album.EventId is not { } eventId)
        {
            return false;
        }

        var @event = await dbContext.Events.AsNoTracking()
            .Where(candidate => candidate.Id == eventId)
            .Select(candidate => new { candidate.OrganizerUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (@event is null)
        {
            return false;
        }

        if (@event.OrganizerUserId == userId)
        {
            return true;
        }

        return await dbContext.EventParticipants.AsNoTracking()
            .AnyAsync(
                participant =>
                    participant.EventId == eventId
                    && participant.UserId == userId
                    && (participant.Status == ParticipantStatus.Approved
                        || participant.Status == ParticipantStatus.Attended),
                cancellationToken);
    }

    internal static async Task<bool> CanViewAsync(
        IApplicationDbContext dbContext,
        Album album,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (viewerUserId is { } viewerId
            && await CanManageAsync(dbContext, album, viewerId, cancellationToken))
        {
            return true;
        }

        if (album.Kind is AlbumKind.Profile && album.OwnerUserId is { } ownerId)
        {
            if (viewerUserId is { } viewer
                && await SocialQueries.BlockedUserIds(dbContext, viewer)
                    .AnyAsync(id => id == ownerId, cancellationToken))
            {
                return false;
            }

            return album.Visibility switch
            {
                AlbumVisibility.Public => true,
                AlbumVisibility.Private => viewerUserId == ownerId,
                AlbumVisibility.Friends => viewerUserId is { } friendViewer
                    && await SocialQueries.AreAcceptedFriendsAsync(
                        dbContext,
                        friendViewer,
                        ownerId,
                        cancellationToken),
                _ => false
            };
        }

        if (album.Kind is AlbumKind.Event && album.EventId is { } eventId)
        {
            var @event = await dbContext.Events.AsNoTracking()
                .Where(candidate => candidate.Id == eventId)
                .Select(candidate => new { candidate.OrganizerUserId })
                .FirstOrDefaultAsync(cancellationToken);

            if (@event is null)
            {
                return false;
            }

            if (viewerUserId is { } viewer
                && await SocialQueries.BlockedUserIds(dbContext, viewer)
                    .AnyAsync(id => id == @event.OrganizerUserId, cancellationToken))
            {
                return false;
            }

            return album.Visibility switch
            {
                AlbumVisibility.Public => true,
                AlbumVisibility.Private => viewerUserId == @event.OrganizerUserId,
                AlbumVisibility.EventParticipants => viewerUserId is { } participantViewer
                    && (@event.OrganizerUserId == participantViewer
                        || await dbContext.EventParticipants.AsNoTracking().AnyAsync(
                            participant =>
                                participant.EventId == eventId
                                && participant.UserId == participantViewer
                                && (participant.Status == ParticipantStatus.Approved
                                    || participant.Status == ParticipantStatus.Attended
                                    || participant.Status == ParticipantStatus.Pending),
                            cancellationToken)),
                _ => false
            };
        }

        return false;
    }
}
