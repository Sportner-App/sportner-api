using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Localization.Resources;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging.SendTextMessage;

public sealed record SendTextMessageCommand(
    Guid ConversationId,
    string Content,
    Guid? ReplyToMessageId = null,
    IReadOnlyList<Guid>? MentionedUserIds = null) : ICommand<MessageResponse>;

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

        if (await MessagingAccess.IsDirectPeerBlockedAsync(
                _dbContext,
                conversation,
                userId,
                cancellationToken))
        {
            return Result<MessageResponse>.Failure(MessagingErrors.Blocked);
        }

        var utcNow = _timeProvider.GetUtcNow();
        await MessagingAccess.CloseIfEventEndedAsync(
            _dbContext,
            conversation,
            utcNow,
            cancellationToken);

        if (conversation.IsClosed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
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

        var mentions = await MessageMentionResolver.ResolveAsync(
            _dbContext, conversation, userId, request.MentionedUserIds, cancellationToken);

        if (mentions.IsFailure)
        {
            return Result<MessageResponse>.Failure(mentions.Errors);
        }

        var message = Message.CreateText(
            conversation.Id,
            userId,
            request.Content,
            utcNow,
            request.ReplyToMessageId);

        message.SetMentions(mentions.Value!, utcNow);
        _dbContext.Messages.Add(message);

        await NotifyOtherMembersAsync(
            conversation,
            userId,
            request.Content,
            mentions.Value!.Select(mention => mention.UserId).ToHashSet(),
            cancellationToken);

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
        IReadOnlySet<Guid> mentionedUserIds,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var preview = content.Length <= 120 ? content : content[..117] + "...";

        // Etiketlenen kişi sessize alma filtresini aşar. Grup sohbetinde
        // etiketlemenin tüm anlamı bu: sohbeti susturmuş biri, doğrudan kendisine
        // seslenildiğinde haberdar olmalı. Önizlemede adı geçtiği için bildirimin
        // neden geldiği de görünür oluyor.
        var recipients = conversation.Members
            .Where(member =>
                member.IsActive()
                && member.UserId != senderUserId
                && (!member.IsMuted(utcNow) || mentionedUserIds.Contains(member.UserId)))
            .Select(member => member.UserId)
            .ToList();

        var senderUsername = await NotificationActor.ResolveUsernameAsync(
            _dbContext, senderUserId, cancellationToken);
        var languagesByRecipient = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, recipients, cancellationToken);

        foreach (var recipientId in recipients)
        {
            var language = languagesByRecipient.GetValueOrDefault(recipientId);
            var displayName = NotificationActor.DisplayName(senderUsername, language);

            // Etkinlik sohbetlerinde yalnızca gönderenin adı yetmiyor: birden
            // fazla etkinliğin sohbetindeki biri, mesajın hangi etkinlikten
            // geldiğini bildirime bakarak anlayamıyordu.
            var title = conversation.Type == ConversationType.Event
                && !string.IsNullOrWhiteSpace(conversation.Title)
                    ? NotificationActor.Format(
                        language,
                        nameof(NotificationsResource.NewMessage_EventTitle),
                        displayName,
                        conversation.Title)
                    : displayName;

            await _notificationPublisher.PublishAsync(
                recipientId,
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
