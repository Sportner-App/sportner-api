namespace Sportner.Application.Features.Messaging;

public sealed record ConversationMemberResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    short Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LastReadAt = null,
    Guid? LastReadMessageId = null);

public sealed record ConversationResponse(
    Guid Id,
    short Type,
    Guid? EventId,
    string? Title,
    bool IsClosed,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    short MyRole,
    IReadOnlyList<ConversationMemberResponse> Members,
    DateTimeOffset? MyMutedUntil = null,
    Guid? MyLastReadMessageId = null,
    DateTimeOffset? MyLastReadAt = null);

public sealed record ConversationListItemResponse(
    Guid Id,
    short Type,
    Guid? EventId,
    string? Title,
    bool IsClosed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    string? LastMessagePreview,
    int UnreadCount = 0,
    bool IsMuted = false,
    bool? IsFriend = null,
    Guid? PeerUserId = null,
    string? PeerUsername = null,
    string? PeerFirstName = null,
    string? PeerProfileImageUrl = null);

public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderUsername,
    string? SenderFirstName,
    short MessageType,
    string? Content,
    string? MediaUrl,
    long? MediaSize,
    string? MediaMimeType,
    Guid? ReplyToMessageId,
    DateTimeOffset? EditedAt,
    bool IsRedacted,
    DateTimeOffset CreatedAt);
