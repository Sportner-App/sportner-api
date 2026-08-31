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
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.EventQuestions.ReplyToEventQuestion;

public sealed record ReplyToEventQuestionCommand(
    Guid EventId,
    Guid QuestionId,
    string Content) : ICommand<EventQuestionResponse>;

public sealed class ReplyToEventQuestionCommandValidator
    : AbstractValidator<ReplyToEventQuestionCommand>
{
    public ReplyToEventQuestionCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.QuestionId).NotEmpty();
        RuleFor(command => command.Content)
            .NotEmpty()
            .MinimumLength(EventQuestion.MinContentLength)
            .MaximumLength(EventQuestion.MaxContentLength);
    }
}

internal sealed class ReplyToEventQuestionCommandHandler
    : ICommandHandler<ReplyToEventQuestionCommand, EventQuestionResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public ReplyToEventQuestionCommandHandler(
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

    public async Task<Result<EventQuestionResponse>> Handle(
        ReplyToEventQuestionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || !user.CanCreateContent())
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.CannotCreateContent);
        }

        var @event = await _dbContext.Events
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);

        if (@event is null)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.EventNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        if (!await EventQuestionAccess.CanWriteAsync(
                _dbContext,
                @event,
                userId,
                utcNow,
                cancellationToken))
        {
            if (@event.HasEnded(utcNow) || @event.Status is EventStatus.Draft or EventStatus.Cancelled)
            {
                return Result<EventQuestionResponse>.Failure(EventQuestionErrors.Closed);
            }

            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.Blocked);
        }

        var target = await _dbContext.EventQuestions
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.Id == request.QuestionId && candidate.EventId == @event.Id,
                cancellationToken);

        if (target is null)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.QuestionNotFound);
        }

        EventQuestion root;
        if (target.IsReply())
        {
            if (target.ParentId is not { } rootId)
            {
                return Result<EventQuestionResponse>.Failure(EventQuestionErrors.QuestionNotFound);
            }

            var resolvedRoot = await _dbContext.EventQuestions
                .FirstOrDefaultAsync(
                    candidate => candidate.Id == rootId && candidate.EventId == @event.Id,
                    cancellationToken);

            if (resolvedRoot is null || resolvedRoot.IsReply())
            {
                return Result<EventQuestionResponse>.Failure(EventQuestionErrors.QuestionNotFound);
            }

            root = resolvedRoot;
        }
        else
        {
            root = target;
        }

        if (target.AuthorUserId != userId
            && await BlockQueries.BlockedPairExistsAsync(
                _dbContext,
                userId,
                target.AuthorUserId,
                cancellationToken))
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.Blocked);
        }

        EventQuestion reply;

        try
        {
            reply = EventQuestion.CreateReply(
                @event.Id,
                userId,
                root.Id,
                request.Content,
                utcNow,
                target.IsReply() ? target.AuthorUserId : null);
        }
        catch (DomainException)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.InvalidContent);
        }

        _dbContext.EventQuestions.Add(reply);
        root.IncrementReplyCount(utcNow);

        var recipients = new HashSet<Guid> { root.AuthorUserId, @event.OrganizerUserId, target.AuthorUserId };
        recipients.Remove(userId);

        var title = await NotificationActor.TitleAsync(
            _dbContext,
            userId,
            "soruna yanıt verdi",
            cancellationToken);
        var preview = EventQuestionAccess.Preview(reply.Content);

        foreach (var recipientId in recipients)
        {
            await _notificationPublisher.PublishAsync(
                recipientId,
                NotificationType.EventQuestionReplied,
                title,
                preview,
                NotificationEntityType.Event,
                @event.Id,
                userId,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var participants = await EventQuestionAccess.ListParticipantUserIdsAsync(
            _dbContext,
            @event.Id,
            cancellationToken);

        return Result<EventQuestionResponse>.Success(
            await EventQuestionAccess.ToResponseAsync(
                _dbContext,
                reply,
                @event.OrganizerUserId,
                participants,
                [],
                cancellationToken));
    }
}
