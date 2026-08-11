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

namespace Sportner.Application.Features.Messaging.CreateGroupConversation;

public sealed record CreateGroupConversationCommand(
    string Title,
    IReadOnlyList<Guid> MemberUserIds) : ICommand<ConversationResponse>;

public sealed class CreateGroupConversationCommandValidator
    : AbstractValidator<CreateGroupConversationCommand>
{
    public CreateGroupConversationCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(100);
        RuleFor(command => command.MemberUserIds).NotNull();
        RuleFor(command => command.MemberUserIds.Count)
            .LessThan(Conversation.MaxGroupMembers)
            .WithMessage($"A group may have at most {Conversation.MaxGroupMembers - 1} invitees.");
    }
}

internal sealed class CreateGroupConversationCommandHandler
    : ICommandHandler<CreateGroupConversationCommand, ConversationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateGroupConversationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ConversationResponse>> Handle(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotAuthenticated);
        }

        var memberIds = request.MemberUserIds
            .Where(id => id != Guid.Empty && id != userId)
            .Distinct()
            .ToList();

        if (memberIds.Count > 0)
        {
            var existingCount = await _dbContext.Users.AsNoTracking()
                .CountAsync(user => memberIds.Contains(user.Id), cancellationToken);

            if (existingCount != memberIds.Count)
            {
                return Result<ConversationResponse>.Failure(MessagingErrors.UserNotFound);
            }

            var friendIds = await SocialQueries.AcceptedFriendIds(_dbContext, userId)
                .ToListAsync(cancellationToken);

            if (memberIds.Any(id => !friendIds.Contains(id)))
            {
                return Result<ConversationResponse>.Failure(MessagingErrors.NotFriends);
            }
        }

        var utcNow = _timeProvider.GetUtcNow();
        Conversation conversation;

        try
        {
            conversation = Conversation.CreateGroupConversation(
                userId,
                request.Title,
                memberIds,
                utcNow);
        }
        catch (DomainException ex) when (ex.Message.Contains("at most", StringComparison.Ordinal))
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.GroupFull);
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
