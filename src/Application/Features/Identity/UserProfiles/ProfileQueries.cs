using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.UserProfiles;

/// <summary>
/// Shared read-side projections for profile endpoints so every response shapes sports and
/// statistics identically.
/// </summary>
internal static class ProfileQueries
{
    /// <summary>
    /// Usernames are stored lowercase so the unique index behaves case-insensitively.
    /// </summary>
    internal const int UsernameChangeCooldownDays = 30;

    internal static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    internal static async Task<IReadOnlyList<ProfileSportResponse>> GetSportsAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken) =>
        await (from userSport in dbContext.UserSports.AsNoTracking()
               join sport in dbContext.Sports.AsNoTracking() on userSport.SportId equals sport.Id
               where userSport.UserId == userId
               orderby userSport.IsPrimary descending, sport.DisplayOrder
               select new ProfileSportResponse(
                   sport.Id,
                   sport.Name,
                   sport.Slug,
                   (short)userSport.SkillLevel,
                   userSport.IsPrimary))
            .ToListAsync(cancellationToken);

    internal static async Task<ProfileStatisticsResponse?> GetStatisticsAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.UserStatistics.AsNoTracking()
            .Where(statistics => statistics.UserId == userId)
            .Select(statistics => new ProfileStatisticsResponse(
                statistics.EventsJoined,
                statistics.EventsOrganized,
                statistics.EventsCompleted,
                statistics.EventsCancelled,
                statistics.AttendanceRate,
                statistics.AverageRating,
                statistics.TotalReviews,
                statistics.FriendsCount,
                statistics.PostsCount,
                statistics.BadgesCount))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Applies visibility rules and loads the shared sections for a public profile lookup.
    /// Owners always see their own profile regardless of the visibility flag.
    /// </summary>
    internal static async Task<Result<PublicProfileResponse>> BuildPublicProfileAsync(
        IApplicationDbContext dbContext,
        UserProfile? profile,
        Guid? requesterId,
        CancellationToken cancellationToken)
    {
        if (profile is null)
        {
            return Result<PublicProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        var isOwner = requesterId == profile.UserId;

        var isReachable = await dbContext.Users.AsNoTracking()
            .AnyAsync(
                user => user.Id == profile.UserId
                    && user.Status != UserStatus.Deleted
                    && user.Status != UserStatus.Banned,
                cancellationToken);

        if (!isReachable)
        {
            return Result<PublicProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        if (requesterId is { } viewerId
            && viewerId != profile.UserId
            && await BlockQueries.BlockedPairExistsAsync(
                dbContext,
                viewerId,
                profile.UserId,
                cancellationToken))
        {
            return Result<PublicProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        if (!profile.IsProfilePublic && !isOwner)
        {
            return Result<PublicProfileResponse>.Failure(ProfileErrors.NotPublic);
        }

        var sports = await GetSportsAsync(dbContext, profile.UserId, cancellationToken);
        var statistics = await GetStatisticsAsync(dbContext, profile.UserId, cancellationToken);
        var friendship = await GetViewerFriendshipAsync(
            dbContext,
            requesterId,
            profile.UserId,
            cancellationToken);

        return Result<PublicProfileResponse>.Success(
            ToPublicProfileResponse(profile, sports, statistics, friendship));
    }

    internal static MyProfileResponse ToMyProfileResponse(
        UserProfile profile,
        IReadOnlyList<ProfileSportResponse> sports,
        ProfileStatisticsResponse? statistics) =>
        new(
            profile.UserId,
            profile.Username,
            profile.FirstName,
            profile.LastName,
            profile.Bio,
            profile.Gender,
            profile.BirthDate,
            profile.City,
            profile.ProfileImageUrl,
            profile.IntroVideoUrl,
            profile.AverageRating,
            profile.ReviewCount,
            profile.IsProfilePublic,
            profile.UsernameChangedAt,
            profile.UsernameChangedAt.AddDays(UsernameChangeCooldownDays),
            sports,
            statistics);

    internal static PublicProfileResponse ToPublicProfileResponse(
        UserProfile profile,
        IReadOnlyList<ProfileSportResponse> sports,
        ProfileStatisticsResponse? statistics,
        ProfileFriendshipResponse? friendship = null) =>
        new(
            profile.UserId,
            profile.Username,
            profile.FirstName,
            profile.LastName,
            profile.Bio,
            profile.City,
            profile.ProfileImageUrl,
            profile.IntroVideoUrl,
            profile.AverageRating,
            profile.ReviewCount,
            sports,
            statistics,
            friendship);

    internal static async Task<ProfileFriendshipResponse?> GetViewerFriendshipAsync(
        IApplicationDbContext dbContext,
        Guid? viewerUserId,
        Guid profileUserId,
        CancellationToken cancellationToken)
    {
        if (viewerUserId is not { } viewerId || viewerId == profileUserId)
        {
            return null;
        }

        var friendship = await dbContext.Friendships.AsNoTracking()
            .FirstOrDefaultAsync(
                candidate =>
                    (candidate.RequesterUserId == viewerId
                        && candidate.AddresseeUserId == profileUserId)
                    || (candidate.RequesterUserId == profileUserId
                        && candidate.AddresseeUserId == viewerId),
                cancellationToken);

        if (friendship is null)
        {
            return null;
        }

        return new ProfileFriendshipResponse(
            friendship.Id,
            (short)friendship.Status,
            friendship.RequesterUserId,
            friendship.AddresseeUserId);
    }
}
