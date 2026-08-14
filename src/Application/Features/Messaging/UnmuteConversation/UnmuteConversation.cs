using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.UnmuteConversation;

public sealed record UnmuteConversationCommand(Guid ConversationId) : ICommand;

internal sealed class UnmuteConversationCommandHandler : ICommandHandler<UnmuteConversationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UnmuteConversationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        UnmuteConversationCommand request,
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

        var conversation = membership.Value!;
        var member = conversation.Members.First(candidate =>
            candidate.UserId == userId && candidate.IsActive());

        member.Unmute(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
