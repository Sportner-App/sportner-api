using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Explore.ExploreEvents;

public sealed record ExploreEventsQuery(
    Guid? SportId = null,
    string? City = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    double? RadiusKm = null,
    int Limit = 20) : IQuery<IReadOnlyList<ExploreEventItemResponse>>;

public sealed class ExploreEventsQueryValidator : AbstractValidator<ExploreEventsQuery>
{
    public ExploreEventsQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, 50);
        RuleFor(query => query.RadiusKm)
            .GreaterThan(0)
            .When(query => query.RadiusKm is not null);
    }
}

internal sealed class ExploreEventsQueryHandler
    : IQueryHandler<ExploreEventsQuery, IReadOnlyList<ExploreEventItemResponse>>
{
    private readonly IRecommendationService _recommendationService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ExploreEventsQueryHandler(
        IRecommendationService recommendationService,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _recommendationService = recommendationService;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ExploreEventItemResponse>>> Handle(
        ExploreEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } viewerId)
        {
            return Result<IReadOnlyList<ExploreEventItemResponse>>.Failure(
                ExploreErrors.NotAuthenticated);
        }

        var scored = await _recommendationService.ScoreEventsAsync(
            viewerId,
            new EventRecommendationRequest(
                request.SportId,
                request.City,
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                request.Limit),
            cancellationToken);

        if (scored.Count == 0)
        {
            return Result<IReadOnlyList<ExploreEventItemResponse>>.Success([]);
        }

        var eventIds = scored.Select(entry => entry.Item.EventId).ToList();

        var details = await (
                from @event in _dbContext.Events.AsNoTracking()
                where eventIds.Contains(@event.Id)
                join sport in _dbContext.Sports.AsNoTracking() on @event.SportId equals sport.Id
                join profile in _dbContext.UserProfiles.AsNoTracking()
                    on @event.OrganizerUserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                select new
                {
                    @event.Id,
                    @event.SportId,
                    SportName = sport.Name,
                    SportSlug = sport.Slug,
                    @event.OrganizerUserId,
                    OrganizerUsername = profile != null ? profile.Username : null,
                    @event.Title,
                    @event.EventDate,
                    @event.DurationMinutes,
                    @event.Address,
                    @event.MaxParticipants,
                    Status = (short)@event.Status,
                    OccupiedParticipantCount = _dbContext.EventParticipants.Count(participant =>
                        participant.EventId == @event.Id
                        && (participant.Status == ParticipantStatus.Pending
                            || participant.Status == ParticipantStatus.Approved
                            || participant.Status == ParticipantStatus.Attended
                            || participant.Status == ParticipantStatus.NoShow))
                })
            .ToListAsync(cancellationToken);

        var detailsById = details.ToDictionary(item => item.Id);

        var items = new List<ExploreEventItemResponse>(scored.Count);
        foreach (var entry in scored)
        {
            if (!detailsById.TryGetValue(entry.Item.EventId, out var detail))
            {
                continue;
            }

            items.Add(new ExploreEventItemResponse(
                detail.Id,
                detail.SportId,
                detail.SportName,
                detail.SportSlug,
                detail.OrganizerUserId,
                detail.OrganizerUsername,
                detail.Title,
                detail.EventDate,
                detail.DurationMinutes,
                detail.Address,
                detail.MaxParticipants,
                detail.Status,
                detail.OccupiedParticipantCount,
                entry.Item.DistanceKm,
                entry.Item.FriendsAttending,
                entry.Item.SportMatch));
        }

        return Result<IReadOnlyList<ExploreEventItemResponse>>.Success(items);
    }
}
