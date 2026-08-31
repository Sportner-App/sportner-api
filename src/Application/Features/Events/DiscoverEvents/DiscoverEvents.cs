using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Geo;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;
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
    short? SkillLevel = null,
    bool? IsPaid = null,
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
        RuleFor(query => query.SkillLevel)
            .Must(level => level is not null && Enum.IsDefined((SkillLevel)level.Value))
            .When(query => query.SkillLevel is not null)
            .WithMessage("Skill level is invalid.");
    }
}

internal sealed class DiscoverEventsQueryHandler
    : IQueryHandler<DiscoverEventsQuery, PagedResult<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DiscoverEventsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
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

        if (_currentUser.UserId is { } viewerId)
        {
            var blockedIds = BlockQueries.BlockedUserIds(_dbContext, viewerId);
            events = events.Where(@event => !blockedIds.Contains(@event.OrganizerUserId));
        }

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

        if (request.SkillLevel is { } skill)
        {
            var skillLevel = (SkillLevel)skill;
            events = events.Where(@event => @event.SkillLevel == skillLevel);
        }

        if (request.IsPaid is { } isPaid)
        {
            events = events.Where(@event => @event.IsPaid == isPaid);
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
                @event.SkillLevel != null ? (short?)@event.SkillLevel : null,
                @event.IsPaid,
                @event.FeeAmount,
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
