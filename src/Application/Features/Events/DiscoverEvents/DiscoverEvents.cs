using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Geo;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.DiscoverEvents;

public sealed record DiscoverEventsQuery(
    Guid? SportId = null,
    string? City = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    double? RadiusKm = null,
    int? MinParticipantAge = null,
    int? MaxParticipantAge = null,
    short? OrganizerGender = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<EventListItemResponse>>;

public sealed class DiscoverEventsQueryValidator : AbstractValidator<DiscoverEventsQuery>
{
    public DiscoverEventsQueryValidator()
    {
        RuleFor(query => query.MinParticipantAge)
            .InclusiveBetween(13, 120)
            .When(query => query.MinParticipantAge is not null);
        RuleFor(query => query.MaxParticipantAge)
            .InclusiveBetween(13, 120)
            .When(query => query.MaxParticipantAge is not null);
        RuleFor(query => query.MaxParticipantAge)
            .GreaterThanOrEqualTo(query => query.MinParticipantAge)
            .When(query => query.MinParticipantAge is not null && query.MaxParticipantAge is not null);
        RuleFor(query => query.OrganizerGender)
            .InclusiveBetween((short)0, (short)2)
            .When(query => query.OrganizerGender is not null);
    }
}

internal sealed class DiscoverEventsQueryHandler
    : IQueryHandler<DiscoverEventsQuery, PagedResult<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DiscoverEventsQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<EventListItemResponse>>> Handle(
        DiscoverEventsQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var utcNow = _timeProvider.GetUtcNow();
        var cityFilter = string.IsNullOrWhiteSpace(request.City)
            ? null
            : request.City.Trim();

        IQueryable<Event> events = _dbContext.Events.AsNoTracking()
            .Where(@event =>
                (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                && @event.EventDate > utcNow);

        if (request.SportId is not null)
        {
            events = events.Where(@event => @event.SportId == request.SportId);
        }

        if (cityFilter is not null)
        {
            var lowered = cityFilter.ToLowerInvariant();
            events = events.Where(@event => @event.Address.ToLower().Contains(lowered));
        }

        if (request.MinParticipantAge is { } minAge)
        {
            events = events.Where(@event => @event.MaxParticipantAge >= minAge);
        }

        if (request.MaxParticipantAge is { } maxAge)
        {
            events = events.Where(@event => @event.MinParticipantAge <= maxAge);
        }

        if (request.OrganizerGender is { } gender)
        {
            events = events.Where(@event => _dbContext.UserProfiles.Any(profile =>
                profile.UserId == @event.OrganizerUserId && profile.Gender == gender));
        }

        if (request.Latitude is { } lat
            && request.Longitude is { } lng
            && request.RadiusKm is { } radiusKm
            && radiusKm > 0)
        {
            var (minLat, maxLat, minLng, maxLng) = GeoBoundingBox.For(lat, lng, radiusKm);
            events = events.Where(@event =>
                @event.Latitude >= minLat
                && @event.Latitude <= maxLat
                && @event.Longitude >= minLng
                && @event.Longitude <= maxLng);
        }

        var query =
            from @event in events
            join sport in _dbContext.Sports.AsNoTracking() on @event.SportId equals sport.Id
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on @event.OrganizerUserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            orderby @event.EventDate
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
                (short)@event.Status,
                _dbContext.EventParticipants.Count(participant =>
                    participant.EventId == @event.Id
                    && (participant.Status == ParticipantStatus.Approved
                        || participant.Status == ParticipantStatus.Attended
                        || participant.Status == ParticipantStatus.NoShow)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<EventListItemResponse>>.Success(
            PagedResult<EventListItemResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
