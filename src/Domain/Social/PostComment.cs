using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class PostComment : AggregateRoot
{
    private const int MaxContentLength = 1000;
    private const int MaxMentions = 20;

    private readonly List<CommentMention> _mentions = [];

    private PostComment()
    {
    }

    public Guid PostId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? ParentCommentId { get; private set; }

    public Guid? ReplyToUserId { get; private set; }

    public string Content { get; private set; } = null!;

    public int LikeCount { get; private set; }

    public int ReplyCount { get; private set; }

    public bool IsHidden { get; private set; }

    public IReadOnlyCollection<CommentMention> Mentions => _mentions.AsReadOnly();

    /// <summary>
    /// Yorumun etiketlerini verilen listeyle değiştirir. Kimin
    /// etiketlenebileceğine (arkadaşlık) uygulama katmanı karar verir.
    /// </summary>
    public void SetMentions(
        IReadOnlyCollection<(Guid UserId, string Username)> mentions,
        DateTimeOffset utcNow)
    {
        var distinct = mentions
            .GroupBy(mention => mention.UserId)
            .Select(group => group.First())
            .ToList();

        if (distinct.Count > MaxMentions)
        {
            throw new DomainException(
                $"A comment may mention a maximum of {MaxMentions} people.");
        }

        if (distinct.Any(mention => mention.UserId == UserId))
        {
            throw new DomainException("A comment cannot mention its own author.");
        }

        _mentions.Clear();

        foreach (var mention in distinct)
        {
            _mentions.Add(CommentMention.Create(Id, mention.UserId, mention.Username, utcNow));
        }

        Touch(utcNow);
    }

    public static PostComment CreateRoot(
        Guid postId,
        Guid userId,
        string content,
        DateTimeOffset utcNow)
    {
        return Create(postId, userId, content, parentCommentId: null, utcNow);
    }

    public static PostComment CreateReply(
        Guid postId,
        Guid userId,
        Guid parentCommentId,
        string content,
        DateTimeOffset utcNow,
        Guid? replyToUserId = null)
    {
        if (parentCommentId == Guid.Empty)
        {
            throw new DomainException("Parent comment id is required.");
        }

        return Create(
            postId,
            userId,
            content,
            parentCommentId,
            utcNow,
            replyToUserId == Guid.Empty ? null : replyToUserId);
    }

    public void UpdateContent(string content, DateTimeOffset utcNow)
    {
        var normalized = NormalizeContent(content);

        if (string.Equals(Content, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Content = normalized;
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

    public void IncrementReplyCount(DateTimeOffset utcNow, int amount = 1)
    {
        ReplyCount = Increment(ReplyCount, amount, "Reply count");
        Touch(utcNow);
    }

    public void DecrementReplyCount(DateTimeOffset utcNow, int amount = 1)
    {
        ReplyCount = Decrement(ReplyCount, amount, "Reply count");
        Touch(utcNow);
    }

    public bool IsReply()
    {
        return ParentCommentId is not null;
    }

    public void Hide(DateTimeOffset utcNow)
    {
        if (IsHidden)
        {
            return;
        }

        IsHidden = true;
        Touch(utcNow);
    }

    public void Unhide(DateTimeOffset utcNow)
    {
        if (!IsHidden)
        {
            return;
        }

        IsHidden = false;
        Touch(utcNow);
    }

    private static PostComment Create(
        Guid postId,
        Guid userId,
        string content,
        Guid? parentCommentId,
        DateTimeOffset utcNow,
        Guid? replyToUserId = null)
    {
        if (postId == Guid.Empty)
        {
            throw new DomainException("Post id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        return new PostComment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            ParentCommentId = parentCommentId,
            ReplyToUserId = replyToUserId,
            Content = NormalizeContent(content),
            LikeCount = 0,
            ReplyCount = 0,
            IsHidden = false,
            CreatedAt = utcNow
        };
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Comment content is required.");
        }

        var normalized = content.Trim();

        if (normalized.Length > MaxContentLength)
        {
            throw new DomainException($"Comment content cannot exceed {MaxContentLength} characters.");
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
