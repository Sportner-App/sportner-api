using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.Common.Geo;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Services.Recommendations;

internal sealed class RecommendationService : IRecommendationService
{
    private static readonly TimeSpan RejectCooldown = TimeSpan.FromDays(30);

    private readonly IApplicationDbContext _dbContext;
    private readonly RecommendationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IApplicationDbContext dbContext,
        IOptions<RecommendationOptions> options,
        TimeProvider timeProvider,
        ILogger<RecommendationService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Scored<RecommendedPerson>>> ScorePeopleAsync(
        Guid viewerUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var utcNow = _timeProvider.GetUtcNow();
        var rejectCutoff = utcNow - RejectCooldown;
        var weights = _options.People;

        var myFriendIds = await SocialQueries.AcceptedFriendIds(_dbContext, viewerUserId)
            .ToListAsync(cancellationToken);
        var myFriendSet = myFriendIds.ToHashSet();

        var myRelationships = await _dbContext.Friendships.AsNoTracking()
            .Where(friendship =>
                friendship.RequesterUserId == viewerUserId
                || friendship.AddresseeUserId == viewerUserId)
            .Select(friendship => new
            {
                friendship.RequesterUserId,
                friendship.AddresseeUserId,
                friendship.Status,
                friendship.RespondedAt
            })
            .ToListAsync(cancellationToken);

        var excluded = new HashSet<Guid> { viewerUserId };
        foreach (var relationship in myRelationships)
        {
            var otherId = relationship.RequesterUserId == viewerUserId
                ? relationship.AddresseeUserId
                : relationship.RequesterUserId;

            if (relationship.Status is FriendshipStatus.Accepted
                or FriendshipStatus.Pending
                or FriendshipStatus.Blocked)
            {
                excluded.Add(otherId);
                continue;
            }

            if (relationship.Status is FriendshipStatus.Rejected
                && relationship.RespondedAt is { } respondedAt
                && respondedAt >= rejectCutoff)
            {
                excluded.Add(otherId);
            }
        }

        foreach (var blockedId in await BlockQueries.BlockedUserIds(_dbContext, viewerUserId)
                     .ToListAsync(cancellationToken))
        {
            excluded.Add(blockedId);
        }

        var mySportIds = await _dbContext.UserSports.AsNoTracking()
            .Where(sport => sport.UserId == viewerUserId)
            .Select(sport => sport.SportId)
            .ToListAsync(cancellationToken);
        var mySportSet = mySportIds.ToHashSet();

        var myCity = NormalizeCity(
            await _dbContext.UserProfiles.AsNoTracking()
                .Where(profile => profile.UserId == viewerUserId)
                .Select(profile => profile.City)
                .FirstOrDefaultAsync(cancellationToken));

        var candidateIds = new HashSet<Guid>();

        if (myFriendIds.Count > 0)
        {
            var friendEdges = await _dbContext.Friendships.AsNoTracking()
                .Where(friendship =>
                    friendship.Status == FriendshipStatus.Accepted
                    && (myFriendSet.Contains(friendship.RequesterUserId)
                        || myFriendSet.Contains(friendship.AddresseeUserId)))
                .Select(friendship => new
                {
                    friendship.RequesterUserId,
                    friendship.AddresseeUserId
                })
                .ToListAsync(cancellationToken);

            foreach (var edge in friendEdges)
            {
                Guid? candidateId = null;
                if (myFriendSet.Contains(edge.RequesterUserId)
                    && !myFriendSet.Contains(edge.AddresseeUserId))
                {
                    candidateId = edge.AddresseeUserId;
                }
                else if (myFriendSet.Contains(edge.AddresseeUserId)
                    && !myFriendSet.Contains(edge.RequesterUserId))
                {
                    candidateId = edge.RequesterUserId;
                }

                if (candidateId is { } id && !excluded.Contains(id))
                {
                    candidateIds.Add(id);
                }
            }
        }

        if (mySportSet.Count > 0)
        {
            var sportCandidates = await _dbContext.UserSports.AsNoTracking()
                .Where(sport => mySportSet.Contains(sport.SportId) && sport.UserId != viewerUserId)
                .Select(sport => sport.UserId)
                .Distinct()
                .Take(_options.PeopleCandidateCap)
                .ToListAsync(cancellationToken);

            foreach (var id in sportCandidates.Where(id => !excluded.Contains(id)))
            {
                candidateIds.Add(id);
            }
        }

        if (myCity is not null)
        {
            var cityCandidates = await _dbContext.UserProfiles.AsNoTracking()
                .Where(profile =>
                    profile.UserId != viewerUserId
                    && profile.City != null
                    && profile.City.ToLower() == myCity)
                .Select(profile => profile.UserId)
                .Take(_options.PeopleCandidateCap)
                .ToListAsync(cancellationToken);

            foreach (var id in cityCandidates.Where(id => !excluded.Contains(id)))
            {
                candidateIds.Add(id);
            }
        }

        // Cold start: if no signals produced candidates, fall back to active public profiles.
        if (candidateIds.Count == 0)
        {
            var fallback = await (
                    from profile in _dbContext.UserProfiles.AsNoTracking()
                    join user in _dbContext.Users.AsNoTracking() on profile.UserId equals user.Id
                    where profile.UserId != viewerUserId
                        && profile.IsProfilePublic
                        && user.Status == UserStatus.Active
                        && !excluded.Contains(profile.UserId)
                    orderby user.CreatedAt descending
                    select profile.UserId)
                .Take(Math.Min(50, _options.PeopleCandidateCap))
                .ToListAsync(cancellationToken);

            foreach (var id in fallback)
            {
                candidateIds.Add(id);
            }
        }

        if (candidateIds.Count == 0)
        {
            return [];
        }

        var cappedIds = candidateIds.Take(_options.PeopleCandidateCap).ToList();

        var profiles = await (
                from profile in _dbContext.UserProfiles.AsNoTracking()
                where cappedIds.Contains(profile.UserId) && profile.IsProfilePublic
                join user in _dbContext.Users.AsNoTracking() on profile.UserId equals user.Id
                where user.Status == UserStatus.Active
                select new
                {
                    profile.UserId,
                    profile.Username,
                    profile.FirstName,
                    profile.ProfileImageUrl,
                    profile.City
                })
            .ToListAsync(cancellationToken);

        var eligibleIds = profiles.Select(profile => profile.UserId).ToList();
        if (eligibleIds.Count == 0)
        {
            return [];
        }

        var mutualCounts = await SocialQueries.CountMutualFriendsAsync(
            _dbContext,
            viewerUserId,
            eligibleIds,
            cancellationToken);
        var sharedSports = await SocialQueries.GetSharedSportNamesAsync(
            _dbContext,
            viewerUserId,
            eligibleIds,
            cancellationToken);

        var stats = await _dbContext.UserStatistics.AsNoTracking()
            .Where(stat => eligibleIds.Contains(stat.UserId))
            .Select(stat => new
            {
                stat.UserId,
                stat.AverageRating,
                stat.AttendanceRate,
                Activity = stat.EventsJoined + stat.PostsCount
            })
            .ToListAsync(cancellationToken);
        var statsById = stats.ToDictionary(stat => stat.UserId);

        var scored = new List<Scored<RecommendedPerson>>(profiles.Count);

        foreach (var profile in profiles)
        {
            var mutual = mutualCounts.GetValueOrDefault(profile.UserId);
            var sports = sharedSports.GetValueOrDefault(profile.UserId) ?? [];
            var sameCity = myCity is not null
                && string.Equals(NormalizeCity(profile.City), myCity, StringComparison.Ordinal);

            statsById.TryGetValue(profile.UserId, out var stat);
            var reputation = (double)(stat?.AverageRating ?? 0m) / 5.0;
            var activity = Math.Min(1.0, (stat?.Activity ?? 0) / 20.0);

            var hasCoreSignal = mutual > 0 || sports.Count > 0 || sameCity;
            var reasons = new List<string>();
            var score = 0.0;

            if (mutual > 0)
            {
                score += weights.MutualFriends * mutual;
                reasons.Add($"mutualFriends:{mutual}");
            }

            if (sports.Count > 0)
            {
                score += weights.SharedSports * sports.Count;
                reasons.Add($"sharedSports:{sports.Count}");
            }

            if (sameCity)
            {
                score += weights.SameCity;
                reasons.Add("sameCity");
            }

            if (reputation > 0)
            {
                score += weights.Reputation * reputation;
                reasons.Add($"reputation:{reputation:F2}");
            }

            if (activity > 0)
            {
                score += weights.Activity * activity;
                reasons.Add($"activity:{activity:F2}");
            }

            if (!hasCoreSignal)
            {
                score = Math.Max(score, 0.01);
                reasons.Add("coldStart");
            }
            else if (score <= 0)
            {
                score = 0.01;
            }

            scored.Add(new Scored<RecommendedPerson>(
                new RecommendedPerson(
                    profile.UserId,
                    profile.Username,
                    profile.FirstName,
                    profile.ProfileImageUrl,
                    profile.City,
                    mutual,
                    sports.Count,
                    sameCity,
                    sports),
                score,
                reasons));
        }

        var result = scored
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Item.Username)
            .Take(limit)
            .ToList();

        _logger.LogDebug(
            "ScorePeople viewer={ViewerId} candidates={Candidates} returned={Returned}",
            viewerUserId,
            eligibleIds.Count,
            result.Count);

        return result;
    }

    public async Task<IReadOnlyList<Scored<RecommendedEvent>>> ScoreEventsAsync(
        Guid viewerUserId,
        EventRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);
        var utcNow = _timeProvider.GetUtcNow();
        var weights = _options.Events;

        var blockedIds = await SocialQueries.BlockedUserIds(_dbContext, viewerUserId)
            .ToListAsync(cancellationToken);
        var blockedSet = blockedIds.ToHashSet();

        var mySportIds = await _dbContext.UserSports.AsNoTracking()
            .Where(sport => sport.UserId == viewerUserId)
            .Select(sport => sport.SportId)
            .ToListAsync(cancellationToken);
        var mySportSet = mySportIds.ToHashSet();

        var friendIds = await SocialQueries.AcceptedFriendIds(_dbContext, viewerUserId)
            .ToListAsync(cancellationToken);
        var friendSet = friendIds.ToHashSet();

        var viewerLocation = await _dbContext.UserSavedLocations.AsNoTracking()
            .Where(location => location.UserId == viewerUserId && location.IsDefault)
            .Select(location => new { location.Latitude, location.Longitude })
            .FirstOrDefaultAsync(cancellationToken);

        var originLat = request.Latitude ?? viewerLocation?.Latitude;
        var originLng = request.Longitude ?? viewerLocation?.Longitude;

        var eventsQuery = _dbContext.Events.AsNoTracking()
            .Where(@event =>
                @event.OrganizationId == null
                && (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                && @event.EventDate > utcNow
                && !blockedSet.Contains(@event.OrganizerUserId));

        if (request.SportId is not null)
        {
            eventsQuery = eventsQuery.Where(@event => @event.SportId == request.SportId);
        }

        if (request.SkillLevel is { } skill)
        {
            var skillLevel = (SkillLevel)skill;
            eventsQuery = eventsQuery.Where(@event => @event.SkillLevel == skillLevel);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLowerInvariant();
            eventsQuery = eventsQuery.Where(@event => @event.Address.ToLower().Contains(city));
        }

        if (originLat is { } lat
            && originLng is { } lng
            && request.RadiusKm is { } radiusKm
            && radiusKm > 0)
        {
            var box = GeoBoundingBox.For(lat, lng, radiusKm);
            eventsQuery = eventsQuery.Where(@event =>
                @event.Latitude >= box.MinLat
                && @event.Latitude <= box.MaxLat
                && @event.Longitude >= box.MinLng
                && @event.Longitude <= box.MaxLng);
        }

        var candidates = await eventsQuery
            .OrderBy(@event => @event.EventDate)
            .Take(_options.EventsCandidateCap)
            .Select(@event => new
            {
                @event.Id,
                @event.SportId,
                @event.OrganizerUserId,
                @event.EventDate,
                @event.Latitude,
                @event.Longitude,
                @event.MaxParticipants
            })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        var eventIds = candidates.Select(candidate => candidate.Id).ToList();

        var participantRows = await _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                eventIds.Contains(participant.EventId)
                && participant.UserId != null
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended))
            .Select(participant => new { participant.EventId, UserId = participant.UserId!.Value })
            .ToListAsync(cancellationToken);

        var participantsByEvent = participantRows
            .GroupBy(row => row.EventId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => row.UserId).ToList());

        var organizerIds = candidates.Select(candidate => candidate.OrganizerUserId).Distinct().ToList();
        var organizerStats = await _dbContext.UserStatistics.AsNoTracking()
            .Where(stat => organizerIds.Contains(stat.UserId))
            .Select(stat => new { stat.UserId, stat.AverageRating })
            .ToListAsync(cancellationToken);
        var organizerRepById = organizerStats.ToDictionary(
            stat => stat.UserId,
            stat => (double)stat.AverageRating / 5.0);

        var scored = new List<Scored<RecommendedEvent>>(candidates.Count);

        foreach (var candidate in candidates)
        {
            participantsByEvent.TryGetValue(candidate.Id, out var participants);
            participants ??= [];

            var participantCount = participants.Count;
            var friendsAttending = participants.Count(id => friendSet.Contains(id));
            var sportMatch = mySportSet.Contains(candidate.SportId);

            double? distanceKm = null;
            if (originLat is { } oLat && originLng is { } oLng)
            {
                distanceKm = GeoBoundingBox.HaversineKm(
                    oLat,
                    oLng,
                    candidate.Latitude,
                    candidate.Longitude);
            }

            var daysUntil = Math.Max(0, (candidate.EventDate - utcNow).TotalDays);
            var timeFit = daysUntil <= 14 ? 1.0 - (daysUntil / 14.0) : 0.15;

            var fillRatio = 0.0;
            if (candidate.MaxParticipants is { } max and > 0)
            {
                var ratio = (double)participantCount / max;
                // Sweet spot around 40–80% filled.
                fillRatio = ratio switch
                {
                    < 0.2 => 0.3,
                    > 0.95 => 0.2,
                    _ => 1.0 - Math.Abs(ratio - 0.6)
                };
            }

            var organizerRep = organizerRepById.GetValueOrDefault(candidate.OrganizerUserId);

            var reasons = new List<string>();
            var score = 0.0;

            if (sportMatch)
            {
                score += weights.SportMatch;
                reasons.Add("sportMatch");
            }

            if (distanceKm is { } distance)
            {
                var distanceScore = distance <= 1 ? 1.0
                    : distance >= 50 ? 0.0
                    : 1.0 - (distance / 50.0);
                score += weights.Distance * distanceScore;
                reasons.Add($"distanceKm:{distance:F1}");
            }

            if (friendsAttending > 0)
            {
                score += weights.FriendsAttending * Math.Min(friendsAttending, 5);
                reasons.Add($"friendsAttending:{friendsAttending}");
            }

            score += weights.TimeFit * timeFit;
            reasons.Add($"timeFit:{timeFit:F2}");

            if (fillRatio > 0)
            {
                score += weights.FillRatio * fillRatio;
                reasons.Add($"fillRatio:{fillRatio:F2}");
            }

            if (organizerRep > 0)
            {
                score += weights.OrganizerRep * organizerRep;
                reasons.Add($"organizerRep:{organizerRep:F2}");
            }

            // Cold start: still surface soonest events.
            if (score <= 0)
            {
                score = timeFit;
                reasons.Add("coldStart");
            }

            scored.Add(new Scored<RecommendedEvent>(
                new RecommendedEvent(
                    candidate.Id,
                    candidate.SportId,
                    candidate.OrganizerUserId,
                    candidate.EventDate,
                    candidate.Latitude,
                    candidate.Longitude,
                    candidate.MaxParticipants,
                    participantCount,
                    friendsAttending,
                    sportMatch,
                    distanceKm),
                score,
                reasons));
        }

        var result = scored
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Item.EventDate)
            .Take(limit)
            .ToList();

        _logger.LogDebug(
            "ScoreEvents viewer={ViewerId} candidates={Candidates} returned={Returned}",
            viewerUserId,
            candidates.Count,
            result.Count);

        return result;
    }

    public async Task<IReadOnlyList<Scored<RecommendedPost>>> ScorePostsAsync(
        Guid viewerUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var utcNow = _timeProvider.GetUtcNow();
        var weights = _options.Posts;

        var blockedIds = await SocialQueries.BlockedUserIds(_dbContext, viewerUserId)
            .ToListAsync(cancellationToken);
        var blockedSet = blockedIds.ToHashSet();

        var friendIds = await SocialQueries.AcceptedFriendIds(_dbContext, viewerUserId)
            .ToListAsync(cancellationToken);
        var friendSet = friendIds.ToHashSet();

        var posts = await _dbContext.Posts.AsNoTracking()
            .Where(post =>
                !post.IsHidden
                && !blockedSet.Contains(post.UserId))
            .OrderByDescending(post => post.CreatedAt)
            .Take(_options.PostsCandidateCap)
            .Select(post => new
            {
                post.Id,
                post.UserId,
                post.CreatedAt,
                post.LikeCount,
                post.CommentCount
            })
            .ToListAsync(cancellationToken);

        if (posts.Count == 0)
        {
            return [];
        }

        var authorIds = posts.Select(post => post.UserId).Distinct().ToList();
        var authorStats = await _dbContext.UserStatistics.AsNoTracking()
            .Where(stat => authorIds.Contains(stat.UserId))
            .Select(stat => new { stat.UserId, stat.AverageRating })
            .ToListAsync(cancellationToken);
        var authorRepById = authorStats.ToDictionary(
            stat => stat.UserId,
            stat => (double)stat.AverageRating / 5.0);

        var scored = new List<Scored<RecommendedPost>>(posts.Count);

        foreach (var post in posts)
        {
            var ageHours = Math.Max(0, (utcNow - post.CreatedAt).TotalHours);
            var recency = Math.Exp(-ageHours / 72.0); // ~3 day half-ish decay
            var engagement = Math.Min(1.0, (post.LikeCount + post.CommentCount * 2) / 30.0);
            var authorIsFriend = friendSet.Contains(post.UserId);
            var authorRep = authorRepById.GetValueOrDefault(post.UserId);

            var reasons = new List<string>();
            var score = 0.0;

            score += weights.Recency * recency;
            reasons.Add($"recency:{recency:F2}");

            if (engagement > 0)
            {
                score += weights.Engagement * engagement;
                reasons.Add($"engagement:{engagement:F2}");
            }

            if (authorIsFriend)
            {
                score += weights.AuthorFriend;
                reasons.Add("authorFriend");
            }

            if (authorRep > 0)
            {
                score += weights.AuthorRep * authorRep;
                reasons.Add($"authorRep:{authorRep:F2}");
            }

            scored.Add(new Scored<RecommendedPost>(
                new RecommendedPost(
                    post.Id,
                    post.UserId,
                    post.CreatedAt,
                    post.LikeCount,
                    post.CommentCount,
                    authorIsFriend),
                score,
                reasons));
        }

        var result = scored
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Item.CreatedAt)
            .Take(limit)
            .ToList();

        _logger.LogDebug(
            "ScorePosts viewer={ViewerId} candidates={Candidates} returned={Returned}",
            viewerUserId,
            posts.Count,
            result.Count);

        return result;
    }

    private static string? NormalizeCity(string? city) =>
        string.IsNullOrWhiteSpace(city) ? null : city.Trim().ToLowerInvariant();
}
