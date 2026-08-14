using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class Album : AggregateRoot
{
    public const int MaxMediaItems = 50;
    public const int MaxAlbumsPerProfile = 20;
    public const int MaxAlbumsPerEvent = 5;

    private readonly List<AlbumMedia> _media = [];

    private Album()
    {
    }

    public AlbumKind Kind { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public Guid? EventId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public AlbumVisibility Visibility { get; private set; }

    public Guid? CoverMediaId { get; private set; }

    public int MediaCount { get; private set; }

    public IReadOnlyCollection<AlbumMedia> Media => _media.AsReadOnly();

    public static Album CreateProfileAlbum(
        Guid ownerUserId,
        string title,
        string? description,
        AlbumVisibility visibility,
        DateTimeOffset utcNow)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainException("Owner user id is required.");
        }

        EnsureProfileVisibility(visibility);

        return new Album
        {
            Id = Guid.NewGuid(),
            Kind = AlbumKind.Profile,
            OwnerUserId = ownerUserId,
            EventId = null,
            Title = NormalizeTitle(title),
            Description = NormalizeDescription(description),
            Visibility = visibility,
            CoverMediaId = null,
            MediaCount = 0,
            CreatedAt = utcNow
        };
    }

    public static Album CreateEventAlbum(
        Guid eventId,
        string title,
        string? description,
        AlbumVisibility? visibility,
        DateTimeOffset utcNow)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        var resolved = visibility ?? AlbumVisibility.EventParticipants;
        EnsureEventVisibility(resolved);

        return new Album
        {
            Id = Guid.NewGuid(),
            Kind = AlbumKind.Event,
            OwnerUserId = null,
            EventId = eventId,
            Title = NormalizeTitle(title),
            Description = NormalizeDescription(description),
            Visibility = resolved,
            CoverMediaId = null,
            MediaCount = 0,
            CreatedAt = utcNow
        };
    }

    public void UpdateDetails(
        string title,
        string? description,
        AlbumVisibility visibility,
        DateTimeOffset utcNow)
    {
        if (Kind is AlbumKind.Profile)
        {
            EnsureProfileVisibility(visibility);
        }
        else
        {
            EnsureEventVisibility(visibility);
        }

        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Visibility = visibility;
        Touch(utcNow);
    }

    public AlbumMedia AddMedia(
        string storagePath,
        string fileName,
        string mimeType,
        long fileSize,
        Guid uploadedByUserId,
        DateTimeOffset utcNow,
        int? width = null,
        int? height = null)
    {
        if (_media.Count >= MaxMediaItems)
        {
            throw new DomainException($"An album may contain a maximum of {MaxMediaItems} media items.");
        }

        var nextOrder = (short)(_media.Count + 1);
        var media = AlbumMedia.Create(
            Id,
            storagePath,
            fileName,
            mimeType,
            fileSize,
            nextOrder,
            uploadedByUserId,
            utcNow,
            width,
            height);

        _media.Add(media);
        MediaCount = _media.Count;
        CoverMediaId ??= media.Id;
        Touch(utcNow);
        return media;
    }

    public string RemoveMedia(Guid mediaId, DateTimeOffset utcNow)
    {
        var media = FindMedia(mediaId);
        var storagePath = media.StoragePath;
        _media.Remove(media);
        ResequenceMedia(utcNow);
        MediaCount = _media.Count;

        if (CoverMediaId == mediaId)
        {
            CoverMediaId = _media.OrderBy(item => item.DisplayOrder).FirstOrDefault()?.Id;
        }

        Touch(utcNow);
        return storagePath;
    }

    public void ReorderMedia(IReadOnlyList<Guid> orderedMediaIds, DateTimeOffset utcNow)
    {
        if (orderedMediaIds.Count != _media.Count
            || orderedMediaIds.Distinct().Count() != orderedMediaIds.Count)
        {
            throw new DomainException("Media reorder list must include each media item exactly once.");
        }

        for (var index = 0; index < orderedMediaIds.Count; index++)
        {
            var media = FindMedia(orderedMediaIds[index]);
            media.ChangeDisplayOrder((short)(index + 1), utcNow);
        }

        Touch(utcNow);
    }

    public void SetCover(Guid mediaId, DateTimeOffset utcNow)
    {
        _ = FindMedia(mediaId);
        if (CoverMediaId == mediaId)
        {
            return;
        }

        CoverMediaId = mediaId;
        Touch(utcNow);
    }

    public IReadOnlyList<string> CollectStoragePaths() =>
        _media.Select(item => item.StoragePath).ToList();

    private AlbumMedia FindMedia(Guid mediaId)
    {
        var media = _media.FirstOrDefault(item => item.Id == mediaId);
        if (media is null)
        {
            throw new DomainException("Album media was not found.");
        }

        return media;
    }

    private void ResequenceMedia(DateTimeOffset utcNow)
    {
        var ordered = _media.OrderBy(item => item.DisplayOrder).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].ChangeDisplayOrder((short)(index + 1), utcNow);
        }
    }

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;

    private static void EnsureProfileVisibility(AlbumVisibility visibility)
    {
        if (!Enum.IsDefined(visibility)
            || visibility is AlbumVisibility.EventParticipants)
        {
            throw new DomainException("Album visibility is invalid for profile albums.");
        }
    }

    private static void EnsureEventVisibility(AlbumVisibility visibility)
    {
        if (!Enum.IsDefined(visibility)
            || visibility is AlbumVisibility.Friends)
        {
            throw new DomainException("Album visibility is invalid for event albums.");
        }
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Album title is required.");
        }

        var normalized = title.Trim();
        if (normalized.Length > 150)
        {
            throw new DomainException("Album title cannot exceed 150 characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        if (normalized.Length > 1000)
        {
            throw new DomainException("Album description cannot exceed 1000 characters.");
        }

        return normalized;
    }
}
