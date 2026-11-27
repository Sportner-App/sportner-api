using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
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
        BlockQueries.BlockedUserIds(dbContext, userId);

    internal static IQueryable<Guid> HiddenUserIds(
        IApplicationDbContext dbContext,
        Guid? viewerId) =>
        BlockQueries.HiddenUserIds(dbContext, viewerId);

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
                profile.FirstName,
                profile.ProfileImageUrl
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
            friendship.CreatedAt,
            requester?.ProfileImageUrl,
            addressee?.ProfileImageUrl);
    }

    internal static async Task<Dictionary<Guid, int>> CountMutualFriendsAsync(
        IApplicationDbContext dbContext,
        Guid viewerUserId,
        IReadOnlyCollection<Guid> otherUserIds,
        CancellationToken cancellationToken)
    {
        var result = otherUserIds.ToDictionary(id => id, _ => 0);
        if (otherUserIds.Count == 0)
        {
            return result;
        }

        var viewerFriends = await AcceptedFriendIds(dbContext, viewerUserId)
            .ToListAsync(cancellationToken);

        if (viewerFriends.Count == 0)
        {
            return result;
        }

        var viewerFriendSet = viewerFriends.ToHashSet();
        var otherIdSet = otherUserIds.ToHashSet();

        var edges = await dbContext.Friendships.AsNoTracking()
            .Where(friendship =>
                friendship.Status == FriendshipStatus.Accepted
                && (otherIdSet.Contains(friendship.RequesterUserId)
                    || otherIdSet.Contains(friendship.AddresseeUserId)))
            .Select(friendship => new
            {
                friendship.RequesterUserId,
                friendship.AddresseeUserId
            })
            .ToListAsync(cancellationToken);

        foreach (var otherUserId in otherUserIds)
        {
            var mutual = 0;

            foreach (var edge in edges)
            {
                Guid friendId;
                if (edge.RequesterUserId == otherUserId)
                {
                    friendId = edge.AddresseeUserId;
                }
                else if (edge.AddresseeUserId == otherUserId)
                {
                    friendId = edge.RequesterUserId;
                }
                else
                {
                    continue;
                }

                if (friendId != viewerUserId && viewerFriendSet.Contains(friendId))
                {
                    mutual++;
                }
            }

            result[otherUserId] = mutual;
        }

        return result;
    }

    internal static async Task<Dictionary<Guid, IReadOnlyList<string>>> GetSharedSportNamesAsync(
        IApplicationDbContext dbContext,
        Guid viewerUserId,
        IReadOnlyCollection<Guid> otherUserIds,
        CancellationToken cancellationToken)
    {
        var empty = (IReadOnlyList<string>)Array.Empty<string>();
        var result = otherUserIds.ToDictionary(id => id, _ => empty);
        if (otherUserIds.Count == 0)
        {
            return result;
        }

        var viewerSportIds = await dbContext.UserSports.AsNoTracking()
            .Where(sport => sport.UserId == viewerUserId)
            .Select(sport => sport.SportId)
            .ToListAsync(cancellationToken);

        if (viewerSportIds.Count == 0)
        {
            return result;
        }

        var viewerSportSet = viewerSportIds.ToHashSet();
        var otherIdSet = otherUserIds.ToHashSet();

        var sharedRows = await (
                from userSport in dbContext.UserSports.AsNoTracking()
                where otherIdSet.Contains(userSport.UserId)
                    && viewerSportSet.Contains(userSport.SportId)
                join sport in dbContext.Sports.AsNoTracking()
                    on userSport.SportId equals sport.Id
                select new { userSport.UserId, sport.Name })
            .ToListAsync(cancellationToken);

        foreach (var group in sharedRows.GroupBy(row => row.UserId))
        {
            result[group.Key] = group
                .Select(row => row.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();
        }

        return result;
    }

    internal static async Task<bool> AreAcceptedFriendsAsync(
        IApplicationDbContext dbContext,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken) =>
        await dbContext.Friendships.AsNoTracking()
            .AnyAsync(
                friendship =>
                    friendship.Status == FriendshipStatus.Accepted
                    && ((friendship.RequesterUserId == firstUserId
                            && friendship.AddresseeUserId == secondUserId)
                        || (friendship.RequesterUserId == secondUserId
                            && friendship.AddresseeUserId == firstUserId)),
                cancellationToken);

    internal static async Task<PostResponse> ToPostResponseAsync(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        Post post,
        Guid? viewerUserId,
        CancellationToken cancellationToken) =>
        (await ToPostResponsesAsync(
            dbContext,
            fileStorage,
            new[] { post },
            viewerUserId,
            cancellationToken))[0];

    internal static async Task<IReadOnlyList<PostResponse>> ToPostResponsesAsync(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        IReadOnlyList<Post> posts,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (posts.Count == 0)
        {
            return Array.Empty<PostResponse>();
        }

        var userIds = posts.Select(post => post.UserId).Distinct().ToList();
        var profileRows = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => userIds.Contains(candidate.UserId))
            .Select(candidate => new
            {
                candidate.UserId,
                candidate.Username,
                candidate.FirstName,
                candidate.ProfileImageUrl
            })
            .ToListAsync(cancellationToken);
        var profiles = new Dictionary<Guid, (string? Username, string? FirstName, string? ProfileImageUrl)>();
        foreach (var row in profileRows)
        {
            profiles.TryAdd(row.UserId, (row.Username, row.FirstName, row.ProfileImageUrl));
        }

        var likedPostIds = new HashSet<Guid>();
        if (viewerUserId is { } viewerId)
        {
            var postIds = posts.Select(post => post.Id).Distinct().ToList();
            var liked = await dbContext.PostLikes.AsNoTracking()
                .Where(like => like.UserId == viewerId && postIds.Contains(like.PostId))
                .Select(like => like.PostId)
                .ToListAsync(cancellationToken);
            likedPostIds.UnionWith(liked);
        }

        // When media wasn't loaded on the aggregate, fall back to a single query.
        var mediaFallbackIds = posts
            .Where(post => post.Media.Count == 0 && post.MediaCount > 0)
            .Select(post => post.Id)
            .Distinct()
            .ToList();
        var fallbackMedia = new Dictionary<Guid, List<PostMediaResponse>>();
        if (mediaFallbackIds.Count > 0)
        {
            var stored = await dbContext.PostMedia.AsNoTracking()
                .Where(item => mediaFallbackIds.Contains(item.PostId))
                .OrderBy(item => item.DisplayOrder)
                .ToListAsync(cancellationToken);

            foreach (var group in stored.GroupBy(item => item.PostId))
            {
                fallbackMedia[group.Key] = group
                    .Select(item => ToPostMediaResponse(fileStorage, item))
                    .ToList();
            }
        }

        // Gonderi etiketler Include edilmeden yuklendiyse koleksiyon bos gelir;
        // Media'daki ayni durum gibi burada da toplu cekiliyor.
        var mentionFallbackIds = posts
            .Where(post => post.Mentions.Count == 0)
            .Select(post => post.Id)
            .Distinct()
            .ToList();
        var fallbackMentions = new Dictionary<Guid, List<PostMentionResponse>>();
        if (mentionFallbackIds.Count > 0)
        {
            var rows = await dbContext.PostMentions.AsNoTracking()
                .Where(mention => mentionFallbackIds.Contains(mention.PostId))
                .Select(mention => new { mention.PostId, mention.UserId, mention.Username })
                .ToListAsync(cancellationToken);

            foreach (var group in rows.GroupBy(row => row.PostId))
            {
                fallbackMentions[group.Key] = group
                    .Select(row => new PostMentionResponse(row.UserId, row.Username))
                    .ToList();
            }
        }

        var eventIds = posts
            .Where(post => post.EventId is not null)
            .Select(post => post.EventId!.Value)
            .Distinct()
            .ToList();
        var eventSnippets = new Dictionary<Guid, PostEventResponse>();
        if (eventIds.Count > 0)
        {
            var snippets = await (
                    from @event in dbContext.Events.AsNoTracking()
                    where eventIds.Contains(@event.Id)
                    join sport in dbContext.Sports.AsNoTracking()
                        on @event.SportId equals sport.Id
                    select new PostEventResponse(
                        @event.Id,
                        @event.Title,
                        sport.Name,
                        sport.Slug,
                        @event.EventDate,
                        @event.Address))
                .ToListAsync(cancellationToken);

            foreach (var snippet in snippets)
            {
                eventSnippets.TryAdd(snippet.Id, snippet);
            }
        }

        var responses = new List<PostResponse>(posts.Count);
        foreach (var post in posts)
        {
            profiles.TryGetValue(post.UserId, out var profile);

            var media = post.Media
                .OrderBy(item => item.DisplayOrder)
                .Select(item => ToPostMediaResponse(fileStorage, item))
                .ToList();
            if (media.Count == 0 && post.MediaCount > 0
                && fallbackMedia.TryGetValue(post.Id, out var storedMedia))
            {
                media = storedMedia;
            }

            var mentions = post.Mentions
                .Select(mention => new PostMentionResponse(mention.UserId, mention.Username))
                .ToList();
            if (mentions.Count == 0 && fallbackMentions.TryGetValue(post.Id, out var storedMentions))
            {
                mentions = storedMentions;
            }

            PostEventResponse? eventSnippet = null;
            if (post.EventId is { } eventId)
            {
                eventSnippets.TryGetValue(eventId, out eventSnippet);
            }

            responses.Add(new PostResponse(
                post.Id,
                post.UserId,
                profile.Username,
                profile.FirstName,
                profile.ProfileImageUrl,
                post.Content,
                post.LikeCount,
                post.CommentCount,
                post.MediaCount,
                likedPostIds.Contains(post.Id),
                post.CreatedAt,
                media,
                mentions,
                eventSnippet));
        }

        return responses;
    }

    private static PostMediaResponse ToPostMediaResponse(IFileStorage fileStorage, PostMedia item) =>
        new(
            item.Id,
            (short)item.MediaType,
            ToClientMediaUrl(fileStorage, item.StoragePath),
            item.FileName,
            item.MimeType,
            item.FileSize,
            item.Width,
            item.Height,
            item.DurationSeconds,
            item.DisplayOrder);

    private static string ToClientMediaUrl(IFileStorage fileStorage, string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return storedPath;
        }

        if (storedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || storedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return storedPath;
        }

        return fileStorage.GetPublicUrl(StorageBuckets.PostMedia, storedPath);
    }
}
