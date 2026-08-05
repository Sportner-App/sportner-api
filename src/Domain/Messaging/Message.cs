using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Messaging;

public class Message : AggregateRoot
{
    private Message()
    {
    }

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public MessageType MessageType { get; private set; }

    public string? Content { get; private set; }

    public string? MediaUrl { get; private set; }

    public long? MediaSize { get; private set; }

    public string? MediaMimeType { get; private set; }

    public Guid? ReplyToMessageId { get; private set; }

    public DateTimeOffset? EditedAt { get; private set; }

    public static Message CreateText(
        Guid conversationId,
        Guid senderUserId,
        string content,
        DateTimeOffset utcNow,
        Guid? replyToMessageId = null)
    {
        EnsureIds(conversationId, senderUserId);

        var normalizedContent = NormalizeRequiredContent(content);

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = MessageType.Text,
            Content = normalizedContent,
            ReplyToMessageId = replyToMessageId,
            CreatedAt = utcNow
        };
    }

    public static Message CreateMedia(
        Guid conversationId,
        Guid senderUserId,
        MessageType messageType,
        string mediaUrl,
        long mediaSize,
        string mediaMimeType,
        DateTimeOffset utcNow,
        string? content = null,
        Guid? replyToMessageId = null)
    {
        EnsureIds(conversationId, senderUserId);

        if (messageType is not (MessageType.Image or MessageType.Video or MessageType.File))
        {
            throw new DomainException("Unsupported media message type.");
        }

        var normalizedMediaUrl = NormalizeRequiredMediaUrl(mediaUrl);
        var normalizedMimeType = NormalizeRequiredMimeType(mediaMimeType);

        if (mediaSize <= 0)
        {
            throw new DomainException("Media size must be greater than zero.");
        }

        var normalizedContent = string.IsNullOrWhiteSpace(content)
            ? null
            : content.Trim();

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = messageType,
            Content = normalizedContent,
            MediaUrl = normalizedMediaUrl,
            MediaSize = mediaSize,
            MediaMimeType = normalizedMimeType,
            ReplyToMessageId = replyToMessageId,
            CreatedAt = utcNow
        };
    }

    public static Message CreateSystem(
        Guid conversationId,
        Guid senderUserId,
        string content,
        DateTimeOffset utcNow)
    {
        EnsureIds(conversationId, senderUserId);

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = MessageType.System,
            Content = NormalizeRequiredContent(content),
            CreatedAt = utcNow
        };
    }

    public void EditContent(string content, DateTimeOffset utcNow)
    {
        if (IsRedacted())
        {
            throw new DomainException("Redacted messages cannot be edited.");
        }

        if (MessageType is not MessageType.Text)
        {
            throw new DomainException("Only text messages can be edited.");
        }

        Content = NormalizeRequiredContent(content);
        EditedAt = utcNow;
        Touch(utcNow);
    }

    public void Redact(DateTimeOffset utcNow)
    {
        if (MessageType is MessageType.System)
        {
            throw new DomainException("System messages cannot be redacted.");
        }

        if (IsRedacted())
        {
            return;
        }

        Content = null;
        MediaUrl = null;
        MediaSize = null;
        MediaMimeType = null;
        Touch(utcNow);
    }

    public bool IsRedacted()
    {
        return MessageType is not MessageType.System
            && Content is null
            && MediaUrl is null
            && MediaSize is null
            && MediaMimeType is null;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static void EnsureIds(Guid conversationId, Guid senderUserId)
    {
        if (conversationId == Guid.Empty)
        {
            throw new DomainException("Conversation id is required.");
        }

        if (senderUserId == Guid.Empty)
        {
            throw new DomainException("Sender user id is required.");
        }
    }

    private static string NormalizeRequiredContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Message content is required.");
        }

        return content.Trim();
    }

    private static string NormalizeRequiredMediaUrl(string mediaUrl)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            throw new DomainException("Media url is required.");
        }

        return mediaUrl.Trim();
    }

    private static string NormalizeRequiredMimeType(string mediaMimeType)
    {
        if (string.IsNullOrWhiteSpace(mediaMimeType))
        {
            throw new DomainException("Media MIME type is required.");
        }

        var normalized = mediaMimeType.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Media MIME type cannot exceed 100 characters.");
        }

        return normalized;
    }
}
