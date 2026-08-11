using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging.SendTextMessage;

public sealed record SendTextMessageCommand(
    Guid ConversationId,
    string Content,
    Guid? ReplyToMessageId = null) : ICommand<MessageResponse>;

public sealed class SendTextMessageCommandValidator : AbstractValidator<SendTextMessageCommand>
{
    public SendTextMessageCommandValidator()
    {
        RuleFor(command => command.ConversationId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(4000);
    }
}

internal sealed class SendTextMessageCommandHandler
    : ICommandHandler<SendTextMessageCommand, MessageResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IChatRealtimeNotifier _chatRealtimeNotifier;

    public SendTextMessageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher,
        IChatRealtimeNotifier chatRealtimeNotifier)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
        _chatRealtimeNotifier = chatRealtimeNotifier;
    }

    public async Task<Result<MessageResponse>> Handle(
        SendTextMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MessageResponse>.Failure(MessagingErrors.NotAuthenticated);
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

        var utcNow = _timeProvider.GetUtcNow();
        var message = Message.CreateText(
            conversation.Id,
            userId,
            request.Content,
            utcNow,
            request.ReplyToMessageId);

        _dbContext.Messages.Add(message);

        await NotifyOtherMembersAsync(conversation, userId, request.Content, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MessageMapping.ToResponseAsync(_dbContext, message, cancellationToken);
        await _chatRealtimeNotifier.NotifyMessageCreatedAsync(
            conversation.Id,
            response,
            cancellationToken);

        return Result<MessageResponse>.Success(response);
    }

    private async Task NotifyOtherMembersAsync(
        Conversation conversation,
        Guid senderUserId,
        string content,
        CancellationToken cancellationToken)
    {
        var preview = content.Length <= 120 ? content : content[..117] + "...";
        var title = conversation.Title ?? "Yeni mesaj";

        foreach (var member in conversation.Members.Where(member =>
                     member.IsActive() && member.UserId != senderUserId))
        {
            await _notificationPublisher.PublishAsync(
                member.UserId,
                NotificationType.NewMessage,
                title,
                preview,
                NotificationEntityType.Conversation,
                conversation.Id,
                senderUserId,
                cancellationToken);
        }
    }
}
