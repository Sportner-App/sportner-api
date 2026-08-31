using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.EventQuestions.AskEventQuestion;

public sealed record AskEventQuestionCommand(Guid EventId, string Content)
    : ICommand<EventQuestionResponse>;

public sealed class AskEventQuestionCommandValidator
    : AbstractValidator<AskEventQuestionCommand>
{
    public AskEventQuestionCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Content)
            .NotEmpty()
            .MinimumLength(EventQuestion.MinContentLength)
            .MaximumLength(EventQuestion.MaxContentLength);
    }
}

internal sealed class AskEventQuestionCommandHandler
    : ICommandHandler<AskEventQuestionCommand, EventQuestionResponse>
{
    private static readonly TimeSpan AskCooldown = TimeSpan.FromSeconds(45);

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public AskEventQuestionCommandHandler(
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
        AskEventQuestionCommand request,
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

        if (@event.OrganizerUserId == userId)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.OrganizerCannotAsk);
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

        var tooFrequent = await _dbContext.EventQuestions.AsNoTracking()
            .AnyAsync(
                question =>
                    question.EventId == @event.Id
                    && question.AuthorUserId == userId
                    && question.ParentId == null
                    && question.CreatedAt >= utcNow - AskCooldown,
                cancellationToken);

        if (tooFrequent)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.TooFrequent);
        }

        EventQuestion question;

        try
        {
            question = EventQuestion.CreateQuestion(@event.Id, userId, request.Content, utcNow);
        }
        catch (DomainException)
        {
            return Result<EventQuestionResponse>.Failure(EventQuestionErrors.InvalidContent);
        }

        _dbContext.EventQuestions.Add(question);

        await _notificationPublisher.PublishAsync(
            @event.OrganizerUserId,
            NotificationType.EventQuestionAsked,
            await NotificationActor.TitleAsync(
                _dbContext,
                userId,
                "etkinliğine soru sordu",
                cancellationToken),
            EventQuestionAccess.Preview(question.Content),
            NotificationEntityType.Event,
            @event.Id,
            userId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var participants = await EventQuestionAccess.ListParticipantUserIdsAsync(
            _dbContext,
            @event.Id,
            cancellationToken);

        return Result<EventQuestionResponse>.Success(
            await EventQuestionAccess.ToResponseAsync(
                _dbContext,
                question,
                @event.OrganizerUserId,
                participants,
                [],
                cancellationToken));
    }
}
