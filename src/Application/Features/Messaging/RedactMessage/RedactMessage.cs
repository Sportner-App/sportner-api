using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Messaging.RedactMessage;

public sealed record RedactMessageCommand(Guid ConversationId, Guid MessageId)
    : ICommand<MessageResponse>;

internal sealed class RedactMessageCommandHandler
    : ICommandHandler<RedactMessageCommand, MessageResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IChatRealtimeNotifier _chatRealtimeNotifier;

    public RedactMessageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage,
        IChatRealtimeNotifier chatRealtimeNotifier)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _fileStorage = fileStorage;
        _chatRealtimeNotifier = chatRealtimeNotifier;
    }

    public async Task<Result<MessageResponse>> Handle(
        RedactMessageCommand request,
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

        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.Id == request.MessageId
                    && candidate.ConversationId == request.ConversationId,
                cancellationToken);

        if (message is null)
        {
            return Result<MessageResponse>.Failure(MessagingErrors.MessageNotFound);
        }

        // Sender can always redact; owner/moderator can redact others' messages.
        if (message.SenderUserId != userId && !conversation.IsOwnerOrModerator(userId))
        {
            return Result<MessageResponse>.Failure(MessagingErrors.NotSender);
        }

        var mediaPath = message.MessageType is MessageType.Image or MessageType.Video or MessageType.File
            ? message.MediaUrl
            : null;

        message.Redact(_timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        await StorageCleanup.TryDeleteAsync(
            _fileStorage,
            StorageBuckets.ChatMedia,
            mediaPath,
            cancellationToken);

        var response = await MessageMapping.ToResponseAsync(_dbContext, message, cancellationToken);
        await _chatRealtimeNotifier.NotifyMessageRedactedAsync(
            conversation.Id,
            response,
            cancellationToken);

        return Result<MessageResponse>.Success(response);
    }
}
