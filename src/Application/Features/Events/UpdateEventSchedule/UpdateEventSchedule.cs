using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.UpdateEventSchedule;

public sealed record UpdateEventScheduleCommand(
    Guid EventId,
    DateTimeOffset EventDate,
    int DurationMinutes) : ICommand<EventResponse>;

public sealed class UpdateEventScheduleCommandValidator : AbstractValidator<UpdateEventScheduleCommand>
{
    public UpdateEventScheduleCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.DurationMinutes).GreaterThan(0);
    }
}

internal sealed class UpdateEventScheduleCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<UpdateEventScheduleCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public UpdateEventScheduleCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventScheduleCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.UpdateSchedule(request.EventDate, request.DurationMinutes, utcNow);

                await EventRosterNotifier.NotifyParticipantsAsync(
                    DbContext,
                    _notificationPublisher,
                    @event,
                    NotificationType.EventScheduleUpdated,
                    nameof(NotificationsResource.EventScheduleUpdated_Title),
                    nameof(NotificationsResource.EventScheduleUpdated_Body),
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
