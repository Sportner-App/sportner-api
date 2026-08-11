using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging.CreateDirectConversation;

public sealed record CreateDirectConversationCommand(Guid OtherUserId)
    : ICommand<ConversationResponse>;

public sealed class CreateDirectConversationCommandValidator
    : AbstractValidator<CreateDirectConversationCommand>
{
    public CreateDirectConversationCommandValidator()
    {
        RuleFor(command => command.OtherUserId).NotEmpty();
    }
}

internal sealed class CreateDirectConversationCommandHandler
    : ICommandHandler<CreateDirectConversationCommand, ConversationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateDirectConversationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ConversationResponse>> Handle(
        CreateDirectConversationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotAuthenticated);
        }

        if (request.OtherUserId == userId)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.CannotMessageSelf);
        }

        var otherExists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.OtherUserId, cancellationToken);

        if (!otherExists)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.PeerNotFound);
        }

        var friendship = await SocialQueries.FindBetweenAsync(
            _dbContext,
            userId,
            request.OtherUserId,
            cancellationToken);

        if (friendship is null || friendship.Status is not FriendshipStatus.Accepted)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotFriends);
        }

        var existing = await MessagingAccess.FindDirectBetweenAsync(
            _dbContext,
            userId,
            request.OtherUserId,
            cancellationToken);

        if (existing is not null)
        {
            var existingResponse = await MessagingAccess.BuildConversationResponseAsync(
                _dbContext,
                existing,
                userId,
                cancellationToken);

            return Result<ConversationResponse>.Success(existingResponse!);
        }

        var utcNow = _timeProvider.GetUtcNow();
        Conversation conversation;

        try
        {
            conversation = Conversation.CreateDirectConversation(
                userId,
                request.OtherUserId,
                utcNow);
        }
        catch (DomainException)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.InvalidOperation);
        }

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MessagingAccess.BuildConversationResponseAsync(
            _dbContext,
            conversation,
            userId,
            cancellationToken);

        return Result<ConversationResponse>.Success(response!);
    }
}
