using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Admin;

public sealed record AdminUserRef(
    Guid Id,
    string? Username,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl);

public sealed record AdminReportTarget(
    bool Exists,
    AdminUserRef? Owner,
    string? Preview,
    string? ImageUrl,
    string State,
    Guid? PostId,
    Guid? EventId);

/// <summary>Panelde gösterilen içerik durumları.</summary>
internal static class AdminContentState
{
    public const string Active = "active";
    public const string Hidden = "hidden";
    public const string Cancelled = "cancelled";
    public const string Redacted = "redacted";
    public const string Flagged = "flagged";
    public const string Banned = "banned";
    public const string Missing = "missing";
}

/// <summary>
/// Şikâyet hedeflerini ve kullanıcı içeriklerini panel için çözer. Engelleme / test hesabı
/// süzgeçleri kullanılmaz: yönetici her şeyi görür. Sorgular türe göre toplu çalışır (N+1 yok).
/// </summary>
internal static class AdminTargets
{
    private const int PreviewLength = 200;

    internal static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= PreviewLength ? trimmed : trimmed[..PreviewLength];
    }

    internal static async Task<Dictionary<Guid, AdminUserRef>> LoadUserRefsAsync(
        IApplicationDbContext dbContext,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Users.AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new AdminUserRef(
                user.Id,
                user.UserProfile != null ? user.UserProfile.Username : null,
                user.UserProfile != null ? user.UserProfile.FirstName : null,
                user.UserProfile != null ? user.UserProfile.LastName : null,
                user.UserProfile != null ? user.UserProfile.ProfileImageUrl : null))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id);
    }

    internal static string PostImageUrl(IFileStorage fileStorage, string storedPath) =>
        storedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || storedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? storedPath
            : fileStorage.GetPublicUrl(StorageBuckets.PostMedia, storedPath);

    /// <summary>Gönderi başına ilk görselin adresi.</summary>
    internal static async Task<Dictionary<Guid, string>> LoadPostImagesAsync(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken)
    {
        if (postIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.PostMedia.AsNoTracking()
            .Where(media => postIds.Contains(media.PostId) && media.MediaType == MediaType.Image)
            .Select(media => new { media.PostId, media.StoragePath, media.DisplayOrder })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.PostId)
            .ToDictionary(
                group => group.Key,
                group => PostImageUrl(fileStorage, group.OrderBy(row => row.DisplayOrder).First().StoragePath));
    }

    internal static async Task<Dictionary<Guid, string>> LoadSportCoversAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> sportIds,
        CancellationToken cancellationToken)
    {
        if (sportIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Sports.AsNoTracking()
            .Where(sport => sportIds.Contains(sport.Id) && sport.CoverImageUrl != null)
            .Select(sport => new { sport.Id, Url = sport.CoverImageUrl! })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.Url);
    }

    internal static bool IsRedacted(MessageType type, string? content, string? mediaUrl, long? mediaSize, string? mimeType) =>
        type is not MessageType.System && content is null && mediaUrl is null && mediaSize is null && mimeType is null;

    internal static async Task<Dictionary<(ReportEntityType Type, Guid Id), AdminReportTarget>> ResolveAsync(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        IEnumerable<(ReportEntityType Type, Guid Id)> keys,
        CancellationToken cancellationToken)
    {
        var distinct = keys.Distinct().ToList();
        var result = new Dictionary<(ReportEntityType, Guid), AdminReportTarget>();

        List<Guid> IdsOf(ReportEntityType type) =>
            distinct.Where(key => key.Type == type).Select(key => key.Id).ToList();

        // Sahip kullanıcı kimlikleri toplanıp tek sorguda çözülür.
        var owners = new Dictionary<(ReportEntityType, Guid), Guid>();
        var partial = new Dictionary<(ReportEntityType, Guid), AdminReportTarget>();

        var userIds = IdsOf(ReportEntityType.User);
        if (userIds.Count > 0)
        {
            var rows = await dbContext.Users.AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new
                {
                    user.Id,
                    user.Status,
                    Username = user.UserProfile != null ? user.UserProfile.Username : null,
                    FirstName = user.UserProfile != null ? user.UserProfile.FirstName : null,
                    LastName = user.UserProfile != null ? user.UserProfile.LastName : null,
                    Image = user.UserProfile != null ? user.UserProfile.ProfileImageUrl : null
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.User, row.Id);
                var name = string.Join(' ', new[] { row.FirstName, row.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
                var label = row.Username is null ? name : $"{name} (@{row.Username})".Trim();
                partial[key] = new AdminReportTarget(
                    true,
                    new AdminUserRef(row.Id, row.Username, row.FirstName, row.LastName, row.Image),
                    Truncate(label),
                    row.Image,
                    row.Status == UserStatus.Banned ? AdminContentState.Banned : AdminContentState.Active,
                    null,
                    null);
            }
        }

        var postIds = IdsOf(ReportEntityType.Post);
        if (postIds.Count > 0)
        {
            var rows = await dbContext.Posts.AsNoTracking()
                .Where(post => postIds.Contains(post.Id))
                .Select(post => new { post.Id, post.UserId, post.Content, post.IsHidden })
                .ToListAsync(cancellationToken);
            var images = await LoadPostImagesAsync(dbContext, fileStorage, rows.Select(row => row.Id).ToList(), cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.Post, row.Id);
                owners[key] = row.UserId;
                partial[key] = new AdminReportTarget(
                    true,
                    null,
                    Truncate(row.Content),
                    images.GetValueOrDefault(row.Id),
                    row.IsHidden ? AdminContentState.Hidden : AdminContentState.Active,
                    null,
                    null);
            }
        }

        var commentIds = IdsOf(ReportEntityType.Comment);
        if (commentIds.Count > 0)
        {
            var rows = await dbContext.PostComments.AsNoTracking()
                .Where(comment => commentIds.Contains(comment.Id))
                .Select(comment => new { comment.Id, comment.UserId, comment.PostId, comment.Content, comment.IsHidden })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.Comment, row.Id);
                owners[key] = row.UserId;
                partial[key] = new AdminReportTarget(
                    true,
                    null,
                    Truncate(row.Content),
                    null,
                    row.IsHidden ? AdminContentState.Hidden : AdminContentState.Active,
                    row.PostId,
                    null);
            }
        }

        var eventIds = IdsOf(ReportEntityType.Event);
        if (eventIds.Count > 0)
        {
            var rows = await dbContext.Events.AsNoTracking()
                .Where(@event => eventIds.Contains(@event.Id))
                .Select(@event => new { @event.Id, @event.OrganizerUserId, @event.Title, @event.Status, @event.SportId })
                .ToListAsync(cancellationToken);
            var covers = await LoadSportCoversAsync(
                dbContext, rows.Select(row => row.SportId).Distinct().ToList(), cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.Event, row.Id);
                owners[key] = row.OrganizerUserId;
                partial[key] = new AdminReportTarget(
                    true,
                    null,
                    Truncate(row.Title),
                    covers.GetValueOrDefault(row.SportId),
                    row.Status == EventStatus.Cancelled ? AdminContentState.Cancelled : AdminContentState.Active,
                    null,
                    row.Id);
            }
        }

        var reviewIds = IdsOf(ReportEntityType.Review);
        if (reviewIds.Count > 0)
        {
            var rows = await dbContext.Reviews.AsNoTracking()
                .Where(review => reviewIds.Contains(review.Id))
                .Select(review => new { review.Id, review.ReviewerUserId, review.Comment, review.IsReported })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.Review, row.Id);
                owners[key] = row.ReviewerUserId;
                partial[key] = new AdminReportTarget(
                    true,
                    null,
                    Truncate(row.Comment),
                    null,
                    row.IsReported ? AdminContentState.Flagged : AdminContentState.Active,
                    null,
                    null);
            }
        }

        var messageIds = IdsOf(ReportEntityType.Message);
        if (messageIds.Count > 0)
        {
            var rows = await dbContext.Messages.AsNoTracking()
                .Where(message => messageIds.Contains(message.Id))
                .Select(message => new
                {
                    message.Id,
                    message.SenderUserId,
                    message.MessageType,
                    message.Content,
                    message.MediaUrl,
                    message.MediaSize,
                    message.MediaMimeType
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.Message, row.Id);
                owners[key] = row.SenderUserId;
                // Gizlilik: mesaj içeriği hiçbir zaman döndürülmez.
                partial[key] = new AdminReportTarget(
                    true,
                    null,
                    null,
                    null,
                    IsRedacted(row.MessageType, row.Content, row.MediaUrl, row.MediaSize, row.MediaMimeType)
                        ? AdminContentState.Redacted
                        : AdminContentState.Active,
                    null,
                    null);
            }
        }

        var albumIds = IdsOf(ReportEntityType.Album);
        if (albumIds.Count > 0)
        {
            var rows = await dbContext.Albums.AsNoTracking()
                .Where(album => albumIds.Contains(album.Id))
                .Select(album => new { album.Id, album.Kind, album.OwnerUserId, album.EventId, album.Title, album.CoverMediaId })
                .ToListAsync(cancellationToken);

            var albumEventIds = rows.Where(row => row.EventId != null).Select(row => row.EventId!.Value).Distinct().ToList();
            var organizers = albumEventIds.Count == 0
                ? []
                : await dbContext.Events.AsNoTracking()
                    .Where(@event => albumEventIds.Contains(@event.Id))
                    .Select(@event => new { @event.Id, @event.OrganizerUserId })
                    .ToDictionaryAsync(row => row.Id, row => row.OrganizerUserId, cancellationToken);

            var coverIds = rows.Where(row => row.CoverMediaId != null).Select(row => row.CoverMediaId!.Value).ToList();
            var covers = coverIds.Count == 0
                ? []
                : await dbContext.AlbumMedia.AsNoTracking()
                    .Where(media => coverIds.Contains(media.Id))
                    .Select(media => new { media.Id, media.StoragePath })
                    .ToDictionaryAsync(row => row.Id, row => row.StoragePath, cancellationToken);

            foreach (var row in rows)
            {
                var key = (ReportEntityType.Album, row.Id);
                var ownerId = row.Kind is AlbumKind.Profile
                    ? row.OwnerUserId
                    : row.EventId is { } albumEventId && organizers.TryGetValue(albumEventId, out var organizerId)
                        ? organizerId
                        : (Guid?)null;
                if (ownerId is { } resolvedOwner)
                {
                    owners[key] = resolvedOwner;
                }

                string? cover = row.CoverMediaId is { } coverId && covers.TryGetValue(coverId, out var path)
                    ? (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? path
                        : fileStorage.GetPublicUrl(StorageBuckets.Albums, path))
                    : null;

                partial[key] = new AdminReportTarget(
                    true,
                    null,
                    Truncate(row.Title),
                    cover,
                    AdminContentState.Active,
                    null,
                    row.EventId);
            }
        }

        var refs = await LoadUserRefsAsync(dbContext, owners.Values, cancellationToken);

        foreach (var key in distinct)
        {
            if (!partial.TryGetValue(key, out var target))
            {
                result[key] = new AdminReportTarget(false, null, null, null, AdminContentState.Missing, null, null);
                continue;
            }

            if (owners.TryGetValue(key, out var ownerId))
            {
                target = target with { Owner = refs.GetValueOrDefault(ownerId) };
            }

            result[key] = target;
        }

        return result;
    }
}
