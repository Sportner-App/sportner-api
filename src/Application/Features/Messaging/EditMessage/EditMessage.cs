using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.EditMessage;

public sealed record EditMessageCommand(Guid ConversationId, Guid MessageId, string Content)
    : ICommand<MessageResponse>;

public sealed class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(command => command.ConversationId).NotEmpty();
        RuleFor(command => command.MessageId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(4000);
    }
}

internal sealed class EditMessageCommandHandler : ICommandHandler<EditMessageCommand, MessageResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IChatRealtimeNotifier _chatRealtimeNotifier;

    public EditMessageCommandHandler(
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

    public async Task<Result<MessageResponse>> Handle(
        EditMessageCommand request,
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

        if (message.SenderUserId != userId)
        {
            return Result<MessageResponse>.Failure(MessagingErrors.NotSender);
        }

        message.EditContent(request.Content, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MessageMapping.ToResponseAsync(_dbContext, message, cancellationToken);
        await _chatRealtimeNotifier.NotifyMessageEditedAsync(
            request.ConversationId,
            response,
            cancellationToken);

        return Result<MessageResponse>.Success(response);
    }
}
