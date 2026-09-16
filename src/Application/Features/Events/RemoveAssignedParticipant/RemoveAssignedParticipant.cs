using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.RemoveAssignedParticipant;

public sealed record RemoveAssignedParticipantCommand(
    Guid EventId,
    Guid ParticipantId,
    Guid ReportReasonId,
    string? Note)
    : ICommand<EventResponse>;

public sealed class RemoveAssignedParticipantCommandValidator
    : FluentValidation.AbstractValidator<RemoveAssignedParticipantCommand>
{
    public RemoveAssignedParticipantCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.ParticipantId).NotEmpty();
        RuleFor(command => command.ReportReasonId).NotEmpty();
        RuleFor(command => command.Note)
            .MaximumLength(EventParticipantRemoval.NoteMaxLength);
    }
}

internal sealed class RemoveAssignedParticipantCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<RemoveAssignedParticipantCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public RemoveAssignedParticipantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        RemoveAssignedParticipantCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                var participant = @event.Participants
                    .FirstOrDefault(item => item.Id == request.ParticipantId);

                if (participant is null)
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                if (participant.UserId == @event.OrganizerUserId)
                {
                    return Result.Failure(EventErrors.NotOrganizer);
                }

                var reasonExists = await DbContext.ReportReasons.AsNoTracking()
                    .AnyAsync(reason =>
                        reason.Id == request.ReportReasonId && reason.IsActive,
                        ct);

                if (!reasonExists)
                {
                    return Result.Failure(EventErrors.RemovalReasonNotFound);
                }

                var userId = participant.UserId;
                var wasApproved = participant.Status is ParticipantStatus.Approved;

                @event.RemoveAssignedParticipant(request.ParticipantId, utcNow);

                DbContext.EventParticipantRemovals.Add(EventParticipantRemoval.Create(
                    @event.Id,
                    participant.Id,
                    @event.OrganizerUserId,
                    userId,
                    request.ReportReasonId,
                    request.Note,
                    utcNow));

                if (userId is { } registeredUserId)
                {
                    await EventAccess.RemoveConversationMemberIfPresentAsync(
                        DbContext,
                        @event.Id,
                        registeredUserId,
                        utcNow,
                        ct);

                    if (wasApproved)
                    {
                        var statistics = await DbContext.UserStatistics
                            .FirstOrDefaultAsync(candidate => candidate.UserId == registeredUserId, ct);

                        if (statistics is not null && statistics.EventsJoined > 0)
                        {
                            statistics.DecreaseEventsJoined(utcNow);
                        }
                    }

                    var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                        DbContext, registeredUserId, ct);

                    await _notificationPublisher.PublishAsync(
                        registeredUserId,
                        NotificationType.EventParticipantRemoved,
                        NotificationActor.Format(
                            recipientLanguage,
                            nameof(NotificationsResource.EventParticipantRemoved_Title),
                            await NotificationActor.PrefixAsync(
                                DbContext, @event.OrganizerUserId, recipientLanguage, ct)),
                        NotificationActor.Format(
                            recipientLanguage,
                            nameof(NotificationsResource.EventParticipantRemoved_Body),
                            @event.Title),
                        NotificationEntityType.Event,
                        @event.Id,
                        @event.OrganizerUserId,
                        ct);
                }

                return Result.Success();
            },
            cancellationToken);
}
