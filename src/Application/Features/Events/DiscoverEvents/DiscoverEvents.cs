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
    Guid? SportCategoryId = null,
    string? City = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    double? RadiusKm = null,
    int? MinParticipantAge = null,
    int? MaxParticipantAge = null,
    short? OrganizerGender = null,
    short? SkillLevel = null,
    bool? IsPaid = null,
    bool FriendsOnly = false,
    bool OrganizationsOnly = false,
    Guid? OrganizationId = null,
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
        RuleFor(query => query.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(query => query.Latitude is not null);
        RuleFor(query => query.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(query => query.Longitude is not null);
        RuleFor(query => query.Longitude)
            .NotNull()
            .When(query => query.Latitude is not null)
            .WithMessage("Longitude is required when latitude is supplied.");
        RuleFor(query => query.Latitude)
            .NotNull()
            .When(query => query.Longitude is not null)
            .WithMessage("Latitude is required when longitude is supplied.");
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

        if ((request.FriendsOnly || request.OrganizationsOnly) && _currentUser.UserId is null)
        {
            return Result<PagedResult<EventListItemResponse>>.Success(
                PagedResult<EventListItemResponse>.Create(
                    [],
                    pagination.NormalizedPage,
                    pagination.NormalizedPageSize,
                    0));
        }

        IQueryable<Event> events = _dbContext.Events.AsNoTracking()
            .Where(@event =>
                (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                && @event.EventDate > utcNow);

        if (request.OrganizationsOnly)
        {
            var myOrganizationIds = _dbContext.OrganizationMembers.AsNoTracking()
                .Where(member =>
                    member.UserId == _currentUser.UserId!.Value
                    && member.Status == OrganizationMemberStatus.Approved)
                .Select(member => member.OrganizationId);

            events = events.Where(@event =>
                @event.OrganizationId != null
                && myOrganizationIds.Contains(@event.OrganizationId.Value));

            if (request.OrganizationId is { } organizationId)
            {
                events = events.Where(@event => @event.OrganizationId == organizationId);
            }
        }
        else
        {
            events = events.Where(@event => @event.OrganizationId == null);
        }

        if (_currentUser.UserId is { } viewerId)
        {
            var blockedIds = BlockQueries.BlockedUserIds(_dbContext, viewerId);
            events = events.Where(@event => !blockedIds.Contains(@event.OrganizerUserId));

            if (request.FriendsOnly)
            {
                var friendIds = SocialQueries.AcceptedFriendIds(_dbContext, viewerId);
                events = events.Where(@event => friendIds.Contains(@event.OrganizerUserId));
            }
        }

        if (request.SportId is not null)
        {
            events = events.Where(@event => @event.SportId == request.SportId);
        }

        if (request.SportCategoryId is { } sportCategoryId)
        {
            events = events.Where(@event => _dbContext.Sports.Any(sport =>
                sport.Id == @event.SportId && sport.CategoryId == sportCategoryId));
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

        var joined =
            from @event in events
            join sport in _dbContext.Sports.AsNoTracking() on @event.SportId equals sport.Id
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on @event.OrganizerUserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            select new { Event = @event, Sport = sport, Profile = profile };

        var total = await joined.CountAsync(cancellationToken);

        // Çağıran kendi konumunu verdiyse varsayılan sıralama yakından uzağa
        // olur. Boylam farkını referans enlemin kosinüsüyle ölçekliyoruz;
        // sonuç yerel ölçekte gerçek mesafeyle monoton artar ve düz SQL
        // aritmetiğine çevrilir, PostGIS gerektirmez. Karekök almıyoruz —
        // sıralama için gereksiz maliyet.
        var hasOrigin = request.Latitude is not null && request.Longitude is not null;
        var originLat = request.Latitude ?? 0m;
        var originLng = request.Longitude ?? 0m;
        var lngScale = (decimal)Math.Cos((double)originLat * Math.PI / 180.0);

        var ordered = hasOrigin
            ? joined
                .OrderBy(row =>
                    (row.Event.Latitude - originLat) * (row.Event.Latitude - originLat)
                    + (row.Event.Longitude - originLng)
                        * (row.Event.Longitude - originLng)
                        * lngScale
                        * lngScale)
                .ThenBy(row => row.Event.EventDate)
                .ThenBy(row => row.Event.Id)
            : joined
                .OrderBy(row => row.Event.EventDate)
                .ThenBy(row => row.Event.Id);

        var items = await ordered
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .Select(row => new EventListItemResponse(
                row.Event.Id,
                row.Event.SportId,
                row.Sport.Name,
                row.Sport.Slug,
                row.Sport.CoverImageUrl,
                row.Event.OrganizerUserId,
                row.Profile != null ? row.Profile.Username : null,
                row.Event.Title,
                row.Event.EventDate,
                row.Event.DurationMinutes,
                row.Event.Latitude,
                row.Event.Longitude,
                row.Event.Address,
                row.Event.MaxParticipants,
                row.Event.MinParticipantAge,
                row.Event.MaxParticipantAge,
                row.Event.SkillLevel != null ? (short?)row.Event.SkillLevel : null,
                row.Event.IsPaid,
                row.Event.FeeAmount,
                (short)row.Event.Status,
                _dbContext.EventParticipants.Count(participant =>
                    participant.EventId == row.Event.Id
                    && (participant.Status == ParticipantStatus.Approved
                        || participant.Status == ParticipantStatus.Attended
                        || participant.Status == ParticipantStatus.NoShow))))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<EventListItemResponse>>.Success(
            PagedResult<EventListItemResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
