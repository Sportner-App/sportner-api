using System.Net;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions;
using Sportner.Application.DTOs.Auth;
using Sportner.Application.DTOs.Events;
using Sportner.Application.Helpers;
using Sportner.Application.Mappers;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;
using Sportner.Domain.Enums;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Services;

public class EventService(
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ICurrentUser currentUser) : IEventService
{
    public async Task<IReadOnlyList<EventDto>> GetEventsAsync(
        EventListQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedTimeframe = (query.Timeframe ?? "upcoming").Trim().ToLowerInvariant();

        if (normalizedTimeframe is not ("upcoming" or "past" or "all"))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_InvalidTimeframe);
        }

        if (query.Latitude.HasValue != query.Longitude.HasValue)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_LatLonRequired);
        }

        if (query.Latitude is < -90 or > 90 || query.Longitude is < -180 or > 180)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_InvalidCoordinates);
        }

        if (query.RadiusKm is <= 0)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_RadiusPositive);
        }

        if (query.RadiusKm is not null && query.Latitude is null)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_RadiusNeedsCoordinates);
        }

        var eventsQuery = unitOfWork.Events
            .AsQueryable()
            .AsNoTracking()
            .Include(e => e.Organizer)
            .AsQueryable();

        eventsQuery = normalizedTimeframe switch
        {
            "past" => eventsQuery.Where(e => e.EventDate < now),
            "all" => eventsQuery,
            _ => eventsQuery.Where(e => e.EventDate >= now)
        };

        if (!string.IsNullOrWhiteSpace(query.SportType))
        {
            eventsQuery = eventsQuery.Where(e => e.SportType == query.SportType);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            eventsQuery = eventsQuery.Where(e =>
                e.Title.ToLower().Contains(term) ||
                (e.Description != null && e.Description.ToLower().Contains(term)) ||
                (e.AddressText != null && e.AddressText.ToLower().Contains(term)));
        }

        if (query.Latitude is not null && query.Longitude is not null && query.RadiusKm is not null)
        {
            var latDelta = query.RadiusKm.Value / 111.0;
            var longitudeScale = Math.Max(
                Math.Abs(Math.Cos(GeoHelper.DegreesToRadians(query.Latitude.Value))),
                0.000001);
            var lonDelta = query.RadiusKm.Value / (111.0 * longitudeScale);

            eventsQuery = eventsQuery.Where(e =>
                e.Latitude >= query.Latitude.Value - latDelta &&
                e.Latitude <= query.Latitude.Value + latDelta &&
                e.Longitude >= query.Longitude.Value - lonDelta &&
                e.Longitude <= query.Longitude.Value + lonDelta);
        }

        eventsQuery = normalizedTimeframe == "past"
            ? eventsQuery.OrderByDescending(e => e.EventDate)
            : eventsQuery.OrderBy(e => e.EventDate);

        var events = await eventsQuery.ToListAsync(cancellationToken);

        if (query.Latitude is not null && query.Longitude is not null)
        {
            var eventsWithDistance = events
                .Select(e => new
                {
                    Event = e,
                    DistanceKm = GeoHelper.HaversineKm(
                        query.Latitude.Value,
                        query.Longitude.Value,
                        e.Latitude,
                        e.Longitude)
                });

            if (query.RadiusKm is not null)
            {
                eventsWithDistance = eventsWithDistance
                    .Where(item => item.DistanceKm <= query.RadiusKm.Value);
            }

            events = eventsWithDistance
                .OrderBy(item => item.DistanceKm)
                .ThenBy(item => item.Event.EventDate)
                .Select(item => item.Event)
                .ToList();
        }

        return events.Select(e => e.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<EventDto>> GetMyPastEventsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        var approvedStatus = ParticipantStatus.Approved.ToDbValue();

        var events = await unitOfWork.Events
            .AsQueryable()
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Where(e => e.EventDate < now)
            .Where(e =>
                e.CreatedBy == userId ||
                e.Participants.Any(p => p.UserId == userId && p.Status == approvedStatus))
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(cancellationToken);

        return events.Select(e => e.ToDto()).ToList();
    }

    public async Task<EventDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await unitOfWork.Events
            .AsQueryable()
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        return eventEntity.ToDetailDto();
    }

    public async Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            SportType = dto.SportType.Trim(),
            EventDate = dto.EventDate.ToUniversalTime(),
            MaxPlayers = dto.MaxPlayers,
            AddressText = string.IsNullOrWhiteSpace(dto.AddressText) ? null : dto.AddressText.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ParticipantsCount = 0
        };

        await unitOfWork.Events.InsertOneAsync(eventEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        eventEntity.Organizer = await unitOfWork.Profiles.FindByIdAsync(userId, cancellationToken);

        return eventEntity.ToDto();
    }

    public async Task<MessageResponseDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var eventEntity = await unitOfWork.Events.FindOneAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        if (eventEntity.CreatedBy != userId)
        {
            throw new ApiException(HttpStatusCode.Forbidden, ValidationResource.Exception_Event_Forbidden);
        }

        unitOfWork.Events.DeleteOne(eventEntity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponseDto(ValidationResource.Exception_Event_Deleted);
    }

    public async Task<ParticipantDto> JoinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var pendingStatus = ParticipantStatus.Pending.ToDbValue();
        var approvedStatus = ParticipantStatus.Approved.ToDbValue();

        var eventEntity = await unitOfWork.Events
            .AsQueryable()
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        if (eventEntity.CreatedBy == userId)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_CannotJoinOwn);
        }

        var existing = eventEntity.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existing is not null)
        {
            if (existing.Status == pendingStatus || existing.Status == approvedStatus)
            {
                throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_AlreadyJoined);
            }

            existing.Status = pendingStatus;
            unitOfWork.EventParticipants.UpdateOne(existing);
        }
        else
        {
            existing = new EventParticipant
            {
                Id = Guid.NewGuid(),
                EventId = eventEntity.Id,
                UserId = userId,
                Status = pendingStatus,
                CreatedAt = DateTime.UtcNow
            };
            await unitOfWork.EventParticipants.InsertOneAsync(existing, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var requester = await unitOfWork.Profiles.FindOneAsync(p => p.Id == userId, cancellationToken);
        var owner = await unitOfWork.Profiles.FindOneAsync(p => p.Id == eventEntity.CreatedBy, cancellationToken);

        var requesterName = string.IsNullOrWhiteSpace(requester?.FullName)
            ? ValidationResource.Notification_RequesterFallback
            : requester!.FullName!;

        await notificationService.SendPushNotificationAsync(
            owner?.PushToken,
            ValidationResource.Notification_JoinRequest_Title,
            string.Format(ValidationResource.Notification_JoinRequest_Body, requesterName, eventEntity.Title),
            new { type = "join_request", eventId = eventEntity.Id, userId },
            cancellationToken);

        return new ParticipantDto(
            existing.UserId,
            requester?.FullName,
            requester?.AvatarUrl,
            SkillLevelHelper.ResolveSkillLevel(requester?.SkillLevels, eventEntity.SportType),
            existing.Status
        );
    }

    public async Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await unitOfWork.Events.FindOneAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        var participants = await unitOfWork.EventParticipants
            .AsQueryable()
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.EventId == id)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return participants.Select(p => p.ToParticipantDto(eventEntity.SportType)).ToList();
    }

    public async Task<ParticipantDto> UpdateParticipantStatusAsync(
        Guid id,
        Guid userId,
        UpdateParticipantStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = RequireUserId();

        if (!ParticipantStatusExtensions.TryParseDbValue(dto.Status, out var parsedStatus) ||
            parsedStatus is not (ParticipantStatus.Approved or ParticipantStatus.Rejected))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_InvalidParticipantStatus);
        }

        var status = parsedStatus.ToDbValue();
        var approvedStatus = ParticipantStatus.Approved.ToDbValue();
        var rejectedStatus = ParticipantStatus.Rejected.ToDbValue();

        var eventEntity = await unitOfWork.Events.FindOneAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        if (eventEntity.CreatedBy != currentUserId)
        {
            throw new ApiException(HttpStatusCode.Forbidden, ValidationResource.Exception_Event_Forbidden);
        }

        var participant = await unitOfWork.EventParticipants
            .AsQueryable()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.EventId == id && p.UserId == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_ParticipantNotFound);

        var wasApproved = participant.Status == approvedStatus;

        if (status == approvedStatus)
        {
            var approvedCount = await unitOfWork.EventParticipants.CountAsync(
                p => p.EventId == id && p.Status == approvedStatus,
                cancellationToken);

            if (!wasApproved && approvedCount >= eventEntity.MaxPlayers)
            {
                throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Event_Full);
            }
        }

        participant.Status = status;

        if (status == approvedStatus && !wasApproved)
        {
            eventEntity.ParticipantsCount += 1;
        }
        else if (status == rejectedStatus && wasApproved)
        {
            eventEntity.ParticipantsCount = Math.Max(0, eventEntity.ParticipantsCount - 1);
        }

        unitOfWork.EventParticipants.UpdateOne(participant);
        unitOfWork.Events.UpdateOne(eventEntity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (status == approvedStatus)
        {
            await notificationService.SendPushNotificationAsync(
                participant.User?.PushToken,
                ValidationResource.Notification_JoinApproved_Title,
                string.Format(ValidationResource.Notification_JoinApproved_Body, eventEntity.Title),
                new { type = "join_approved", eventId = eventEntity.Id },
                cancellationToken);
        }
        else
        {
            await notificationService.SendPushNotificationAsync(
                participant.User?.PushToken,
                ValidationResource.Notification_JoinRejected_Title,
                string.Format(ValidationResource.Notification_JoinRejected_Body, eventEntity.Title),
                new { type = "join_rejected", eventId = eventEntity.Id },
                cancellationToken);
        }

        return participant.ToParticipantDto(eventEntity.SportType);
    }

    private Guid RequireUserId() =>
        currentUser.UserId
        ?? throw new ApiException(HttpStatusCode.Unauthorized, ValidationResource.Exception_Unauthorized);
}
