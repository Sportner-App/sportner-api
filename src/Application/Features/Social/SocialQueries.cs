using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social;

internal static class SocialQueries
{
    internal static Task<Friendship?> FindBetweenAsync(
        IApplicationDbContext dbContext,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken) =>
        dbContext.Friendships
            .FirstOrDefaultAsync(
                friendship =>
                    (friendship.RequesterUserId == firstUserId
                        && friendship.AddresseeUserId == secondUserId)
                    || (friendship.RequesterUserId == secondUserId
                        && friendship.AddresseeUserId == firstUserId),
                cancellationToken);

    internal static IQueryable<Guid> AcceptedFriendIds(
        IApplicationDbContext dbContext,
        Guid userId) =>
        dbContext.Friendships.AsNoTracking()
            .Where(friendship =>
                friendship.Status == FriendshipStatus.Accepted
                && (friendship.RequesterUserId == userId
                    || friendship.AddresseeUserId == userId))
            .Select(friendship =>
                friendship.RequesterUserId == userId
                    ? friendship.AddresseeUserId
                    : friendship.RequesterUserId);

    internal static IQueryable<Guid> BlockedUserIds(
        IApplicationDbContext dbContext,
        Guid userId) =>
        dbContext.Friendships.AsNoTracking()
            .Where(friendship =>
                friendship.Status == FriendshipStatus.Blocked
                && (friendship.RequesterUserId == userId
                    || friendship.AddresseeUserId == userId))
            .Select(friendship =>
                friendship.RequesterUserId == userId
                    ? friendship.AddresseeUserId
                    : friendship.RequesterUserId);

    internal static async Task<FriendshipResponse> ToFriendshipResponseAsync(
        IApplicationDbContext dbContext,
        Friendship friendship,
        CancellationToken cancellationToken)
    {
        var profiles = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile =>
                profile.UserId == friendship.RequesterUserId
                || profile.UserId == friendship.AddresseeUserId)
            .Select(profile => new
            {
                profile.UserId,
                profile.Username,
                profile.FirstName
            })
            .ToListAsync(cancellationToken);

        var requester = profiles.FirstOrDefault(profile => profile.UserId == friendship.RequesterUserId);
        var addressee = profiles.FirstOrDefault(profile => profile.UserId == friendship.AddresseeUserId);

        return new FriendshipResponse(
            friendship.Id,
            friendship.RequesterUserId,
            requester?.Username,
            requester?.FirstName,
            friendship.AddresseeUserId,
            addressee?.Username,
            addressee?.FirstName,
            (short)friendship.Status,
            friendship.RespondedAt,
            friendship.CreatedAt);
    }

    internal static async Task<PostResponse> ToPostResponseAsync(
        IApplicationDbContext dbContext,
        Post post,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == post.UserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        var likedByMe = viewerUserId is not null
            && await dbContext.PostLikes.AsNoTracking()
                .AnyAsync(
                    like => like.PostId == post.Id && like.UserId == viewerUserId,
                    cancellationToken);

        var media = post.Media
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new PostMediaResponse(
                item.Id,
                (short)item.MediaType,
                item.StoragePath,
                item.FileName,
                item.MimeType,
                item.FileSize,
                item.Width,
                item.Height,
                item.DurationSeconds,
                item.DisplayOrder))
            .ToList();

        // When media wasn't loaded on the aggregate, fall back to a query.
        if (media.Count == 0 && post.MediaCount > 0)
        {
            media = await dbContext.PostMedia.AsNoTracking()
                .Where(item => item.PostId == post.Id)
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new PostMediaResponse(
                    item.Id,
                    (short)item.MediaType,
                    item.StoragePath,
                    item.FileName,
                    item.MimeType,
                    item.FileSize,
                    item.Width,
                    item.Height,
                    item.DurationSeconds,
                    item.DisplayOrder))
                .ToListAsync(cancellationToken);
        }

        return new PostResponse(
            post.Id,
            post.UserId,
            profile?.Username,
            profile?.FirstName,
            profile?.ProfileImageUrl,
            post.Content,
            post.LikeCount,
            post.CommentCount,
            post.MediaCount,
            likedByMe,
            post.CreatedAt,
            media);
    }
}
