using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging.SendMediaMessage;

public sealed record SendMediaMessageCommand(
    Guid ConversationId,
    Stream Content,
    string ContentType,
    string FileName,
    string? Caption = null,
    Guid? ReplyToMessageId = null) : ICommand<MessageResponse>;

internal sealed class SendMediaMessageCommandHandler
    : ICommandHandler<SendMediaMessageCommand, MessageResponse>
{
    private static readonly Dictionary<string, MessageType> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = MessageType.Image,
        ["image/png"] = MessageType.Image,
        ["image/webp"] = MessageType.Image,
        ["video/mp4"] = MessageType.Video,
        ["video/quicktime"] = MessageType.Video,
        ["video/webm"] = MessageType.Video,
        ["application/pdf"] = MessageType.File
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IChatRealtimeNotifier _chatRealtimeNotifier;

    public SendMediaMessageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage,
        INotificationPublisher notificationPublisher,
        IChatRealtimeNotifier chatRealtimeNotifier)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _fileStorage = fileStorage;
        _notificationPublisher = notificationPublisher;
        _chatRealtimeNotifier = chatRealtimeNotifier;
    }

    public async Task<Result<MessageResponse>> Handle(
        SendMediaMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MessageResponse>.Failure(MessagingErrors.NotAuthenticated);
        }

        if (!AllowedContentTypes.TryGetValue(request.ContentType, out var messageType))
        {
            return Result<MessageResponse>.Failure(MessagingErrors.InvalidMedia);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            request.ConversationId,
            userId,
            cancellationToken);

        if (membership.IsFailure)
        {
            return Result<MessageResponse>.Failure(membership.Errors);
        }

        var conversation = membership.Value!;

        if (conversation.IsClosed)
        {
            return Result<MessageResponse>.Failure(MessagingErrors.ConversationClosed);
        }

        if (!conversation.CanUserSendMessage(userId))
        {
            return Result<MessageResponse>.Failure(MessagingErrors.CannotSend);
        }

        if (request.ReplyToMessageId is not null)
        {
            var replyExists = await _dbContext.Messages.AsNoTracking()
                .AnyAsync(
                    message =>
                        message.Id == request.ReplyToMessageId
                        && message.ConversationId == request.ConversationId,
                    cancellationToken);

            if (!replyExists)
            {
                return Result<MessageResponse>.Failure(MessagingErrors.ReplyNotFound);
            }
        }

        var extension = Path.GetExtension(request.FileName);
        var objectPath = $"{conversation.Id}/{userId}/{Guid.NewGuid():N}{extension}";
        var mediaSize = request.Content.CanSeek ? request.Content.Length : 0;

        var storedPath = await _fileStorage.UploadAsync(
            StorageBuckets.ChatMedia,
            objectPath,
            request.Content,
            request.ContentType,
            cancellationToken);

        if (mediaSize <= 0)
        {
            // Upload succeeded; size is required by the domain — use a positive placeholder
            // only when the stream cannot report length (should be rare for IFormFile).
            mediaSize = 1;
        }

        var utcNow = _timeProvider.GetUtcNow();
        var message = Message.CreateMedia(
            conversation.Id,
            userId,
            messageType,
            storedPath,
            mediaSize,
            request.ContentType,
            utcNow,
            request.Caption,
            request.ReplyToMessageId);

        _dbContext.Messages.Add(message);

        var preview = request.Caption is { Length: > 0 }
            ? request.Caption
            : "Yeni medya mesajı";

        foreach (var member in conversation.Members.Where(member =>
                     member.IsActive() && member.UserId != userId))
        {
            await _notificationPublisher.PublishAsync(
                member.UserId,
                NotificationType.NewMessage,
                conversation.Title ?? "Yeni mesaj",
                preview,
                NotificationEntityType.Conversation,
                conversation.Id,
                userId,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MessageMapping.ToResponseAsync(_dbContext, message, cancellationToken);
        await _chatRealtimeNotifier.NotifyMessageCreatedAsync(
            conversation.Id,
            response,
            cancellationToken);

        return Result<MessageResponse>.Success(response);
    }
}
