using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Messaging.InviteConversationMember;

public sealed record InviteConversationMemberCommand(Guid ConversationId, Guid UserId)
    : ICommand<ConversationResponse>;

public sealed class InviteConversationMemberCommandValidator
    : AbstractValidator<InviteConversationMemberCommand>
{
    public InviteConversationMemberCommandValidator()
    {
        RuleFor(command => command.ConversationId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}

internal sealed class InviteConversationMemberCommandHandler
    : ICommandHandler<InviteConversationMemberCommand, ConversationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public InviteConversationMemberCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<ConversationResponse>> Handle(
        InviteConversationMemberCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotAuthenticated);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            request.ConversationId,
            userId,
            cancellationToken);

        if (membership.IsFailure)
        {
            return Result<ConversationResponse>.Failure(membership.Errors);
        }

        var conversation = membership.Value!;

        if (conversation.Type is not ConversationType.Group)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.CannotInvite);
        }

        var inviteeExists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(
                user => user.Id == request.UserId
                    && user.Status != UserStatus.Deleted
                    && user.Status != UserStatus.Banned,
                cancellationToken);

        if (!inviteeExists)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.PeerNotFound);
        }

        var friendship = await SocialQueries.FindBetweenAsync(
            _dbContext,
            userId,
            request.UserId,
            cancellationToken);

        if (await BlockQueries.BlockedPairExistsAsync(
                _dbContext,
                userId,
                request.UserId,
                cancellationToken))
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.Blocked);
        }

        if (friendship is null || friendship.Status is not FriendshipStatus.Accepted)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotFriends);
        }

        try
        {
            conversation.InviteMember(userId, request.UserId, _timeProvider.GetUtcNow());
        }
        catch (DomainException ex) when (ex.Message.Contains("at most", StringComparison.Ordinal))
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.GroupFull);
        }
        catch (DomainException)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.CannotInvite);
        }

        var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
            _dbContext, request.UserId, cancellationToken);

        await _notificationPublisher.PublishAsync(
            request.UserId,
            NotificationType.ConversationMemberAdded,
            NotificationActor.Format(
                recipientLanguage,
                nameof(NotificationsResource.ConversationMemberAdded_Title),
                await NotificationActor.PrefixAsync(_dbContext, userId, recipientLanguage, cancellationToken)),
            NotificationActor.Format(
                recipientLanguage,
                nameof(NotificationsResource.ConversationMemberAdded_Body)),
            NotificationEntityType.Conversation,
            conversation.Id,
            userId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MessagingAccess.BuildConversationResponseAsync(
            _dbContext,
            conversation,
            userId,
            cancellationToken);

        return Result<ConversationResponse>.Success(response!);
    }
}
