using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.DeleteConversation;

public sealed record DeleteConversationCommand(Guid ConversationId) : ICommand;

internal sealed class DeleteConversationCommandHandler
    : ICommandHandler<DeleteConversationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IChatRealtimeNotifier _chatRealtimeNotifier;

    public DeleteConversationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IChatRealtimeNotifier chatRealtimeNotifier)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _chatRealtimeNotifier = chatRealtimeNotifier;
    }

    public async Task<Result> Handle(
        DeleteConversationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(MessagingErrors.NotAuthenticated);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            request.ConversationId,
            userId,
            cancellationToken);

        if (membership.IsFailure)
        {
            return Result.Failure(membership.Errors);
        }

        var member = membership.Value!.Members.First(candidate =>
            candidate.UserId == userId && candidate.IsActive());
        member.ClearHistory(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _chatRealtimeNotifier.NotifyConversationUpdatedAsync(
            request.ConversationId,
            [userId],
            cancellationToken);

        return Result.Success();
    }
}
