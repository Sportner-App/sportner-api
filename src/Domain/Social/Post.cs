using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class Post : AggregateRoot
{
    private const int MaxContentLength = 2200;
    private const int MaxMediaItems = 10;

    private readonly List<PostMedia> _media = [];

    private Post()
    {
    }

    public Guid UserId { get; private set; }

    public string? Content { get; private set; }

    public int LikeCount { get; private set; }

    public int CommentCount { get; private set; }

    public short MediaCount { get; private set; }

    public IReadOnlyCollection<PostMedia> Media => _media.AsReadOnly();

    public static Post Create(Guid userId, string? content, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        return new Post
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = NormalizeContent(content),
            LikeCount = 0,
            CommentCount = 0,
            MediaCount = 0,
            CreatedAt = utcNow
        };
    }

    public void UpdateContent(string? content, DateTimeOffset utcNow)
    {
        var normalized = NormalizeContent(content);

        if (normalized is null && _media.Count == 0)
        {
            throw new DomainException("Post must contain content or media.");
        }

        if (string.Equals(Content, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Content = normalized;
        Touch(utcNow);
    }

    public PostMedia AddMedia(
        MediaType mediaType,
        string storagePath,
        string fileName,
        string mimeType,
        long fileSize,
        DateTimeOffset utcNow,
        int? width = null,
        int? height = null,
        int? durationSeconds = null)
    {
        if (_media.Count >= MaxMediaItems)
        {
            throw new DomainException($"A post may contain a maximum of {MaxMediaItems} media items.");
        }

        var nextOrder = (short)(_media.Count + 1);
        var media = PostMedia.Create(
            Id,
            mediaType,
            storagePath,
            fileName,
            mimeType,
            fileSize,
            nextOrder,
            utcNow,
            width,
            height,
            durationSeconds);

        _media.Add(media);
        MediaCount = (short)_media.Count;
        Touch(utcNow);

        return media;
    }

    public string RemoveMedia(Guid mediaId, DateTimeOffset utcNow)
    {
        var media = FindMedia(mediaId);

        if (Content is null && _media.Count == 1)
        {
            throw new DomainException("Post must contain content or media.");
        }

        var storagePath = media.StoragePath;
        _media.Remove(media);
        ResequenceMedia(utcNow);
        MediaCount = (short)_media.Count;
        Touch(utcNow);

        return storagePath;
    }

    public void ReorderMedia(IReadOnlyList<Guid> orderedMediaIds, DateTimeOffset utcNow)
    {
        if (orderedMediaIds.Count != _media.Count)
        {
            throw new DomainException("Reorder list must include every media item exactly once.");
        }

        if (orderedMediaIds.Distinct().Count() != orderedMediaIds.Count)
        {
            throw new DomainException("Reorder list contains duplicate media ids.");
        }

        var mediaById = _media.ToDictionary(item => item.Id);

        foreach (var mediaId in orderedMediaIds)
        {
            if (!mediaById.ContainsKey(mediaId))
            {
                throw new DomainException("Reorder list contains an unknown media id.");
            }
        }

        for (var index = 0; index < orderedMediaIds.Count; index++)
        {
            mediaById[orderedMediaIds[index]].ChangeDisplayOrder((short)(index + 1), utcNow);
        }

        Touch(utcNow);
    }

    public void IncrementLikeCount(DateTimeOffset utcNow, int amount = 1)
    {
        LikeCount = Increment(LikeCount, amount, "Like count");
        Touch(utcNow);
    }

    public void DecrementLikeCount(DateTimeOffset utcNow, int amount = 1)
    {
        LikeCount = Decrement(LikeCount, amount, "Like count");
        Touch(utcNow);
    }

    public void IncrementCommentCount(DateTimeOffset utcNow, int amount = 1)
    {
        CommentCount = Increment(CommentCount, amount, "Comment count");
        Touch(utcNow);
    }

    public void DecrementCommentCount(DateTimeOffset utcNow, int amount = 1)
    {
        CommentCount = Decrement(CommentCount, amount, "Comment count");
        Touch(utcNow);
    }

    public void ValidatePublishable()
    {
        if (!HasContent() && !HasMedia())
        {
            throw new DomainException("Post must contain content or media.");
        }
    }

    public bool HasContent()
    {
        return Content is not null;
    }

    public bool HasMedia()
    {
        return _media.Count > 0;
    }

    private PostMedia FindMedia(Guid mediaId)
    {
        var media = _media.FirstOrDefault(item => item.Id == mediaId);

        if (media is null)
        {
            throw new DomainException("Post media was not found.");
        }

        if (media.PostId != Id)
        {
            throw new DomainException("Post media does not belong to this post.");
        }

        return media;
    }

    private void ResequenceMedia(DateTimeOffset utcNow)
    {
        var ordered = _media
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.CreatedAt)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].ChangeDisplayOrder((short)(index + 1), utcNow);
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string? NormalizeContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var normalized = content.Trim();

        if (normalized.Length > MaxContentLength)
        {
            throw new DomainException($"Post content cannot exceed {MaxContentLength} characters.");
        }

        return normalized;
    }

    private static int Increment(int value, int amount, string fieldName)
    {
        if (amount <= 0)
        {
            throw new DomainException($"{fieldName} increment amount must be greater than zero.");
        }

        if (value > int.MaxValue - amount)
        {
            throw new DomainException($"{fieldName} overflow.");
        }

        return value + amount;
    }

    private static int Decrement(int value, int amount, string fieldName)
    {
        if (amount <= 0)
        {
            throw new DomainException($"{fieldName} decrement amount must be greater than zero.");
        }

        if (value < amount)
        {
            throw new DomainException($"{fieldName} cannot become negative.");
        }

        return value - amount;
    }
}
