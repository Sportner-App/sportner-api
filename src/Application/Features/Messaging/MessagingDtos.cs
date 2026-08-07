namespace Sportner.Application.Features.Messaging;

public sealed record ConversationMemberResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    short Role,
    DateTimeOffset JoinedAt);

public sealed record ConversationResponse(
    Guid Id,
    short Type,
    Guid? EventId,
    string? Title,
    bool IsClosed,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    short MyRole,
    IReadOnlyList<ConversationMemberResponse> Members);

public sealed record ConversationListItemResponse(
    Guid Id,
    short Type,
    Guid? EventId,
    string? Title,
    bool IsClosed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    string? LastMessagePreview);

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
