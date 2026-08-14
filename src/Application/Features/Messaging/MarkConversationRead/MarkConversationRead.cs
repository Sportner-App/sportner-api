using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Messaging.MarkConversationRead;

public sealed record MarkConversationReadCommand(Guid ConversationId, Guid MessageId) : ICommand;

public sealed class MarkConversationReadCommandValidator
    : AbstractValidator<MarkConversationReadCommand>
{
    public MarkConversationReadCommandValidator()
    {
        RuleFor(command => command.ConversationId).NotEmpty();
        RuleFor(command => command.MessageId).NotEmpty();
    }
}

internal sealed class MarkConversationReadCommandHandler
    : ICommandHandler<MarkConversationReadCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public MarkConversationReadCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        MarkConversationReadCommand request,
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

        var message = await _dbContext.Messages.AsNoTracking()
            .Where(candidate =>
                candidate.Id == request.MessageId
                && candidate.ConversationId == request.ConversationId)
            .Select(candidate => new { candidate.Id, candidate.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (message is null)
        {
            return Result.Failure(MessagingErrors.MessageNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();

        try
        {
            member.MarkRead(message.Id, message.CreatedAt, utcNow);
        }
        catch (DomainException)
        {
            return Result.Failure(MessagingErrors.InvalidOperation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
