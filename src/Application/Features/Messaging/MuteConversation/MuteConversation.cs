using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Messaging.MuteConversation;

public sealed record MuteConversationCommand(Guid ConversationId, DateTimeOffset? Until = null)
    : ICommand;

public sealed class MuteConversationCommandValidator : AbstractValidator<MuteConversationCommand>
{
    public MuteConversationCommandValidator()
    {
        RuleFor(command => command.ConversationId).NotEmpty();
    }
}

internal sealed class MuteConversationCommandHandler : ICommandHandler<MuteConversationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public MuteConversationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        MuteConversationCommand request,
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

        var utcNow = _timeProvider.GetUtcNow();
        var until = request.Until ?? DateTimeOffset.MaxValue;

        try
        {
            member.Mute(until, utcNow);
        }
        catch (DomainException)
        {
            return Result.Failure(MessagingErrors.InvalidOperation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
