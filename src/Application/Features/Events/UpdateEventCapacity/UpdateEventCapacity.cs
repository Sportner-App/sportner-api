using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.UpdateEventCapacity;

public sealed record UpdateEventCapacityCommand(Guid EventId, int? MaxParticipants)
    : ICommand<EventResponse>;

public sealed class UpdateEventCapacityCommandValidator : AbstractValidator<UpdateEventCapacityCommand>
{
    public UpdateEventCapacityCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.MaxParticipants)
            .GreaterThan(0)
            .When(command => command.MaxParticipants is not null);
    }
}

internal sealed class UpdateEventCapacityCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<UpdateEventCapacityCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public UpdateEventCapacityCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventCapacityCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.UpdateCapacity(request.MaxParticipants, utcNow);

                await EventRosterNotifier.NotifyParticipantsAsync(
                    DbContext,
                    _notificationPublisher,
                    @event,
                    NotificationType.EventCapacityUpdated,
                    nameof(NotificationsResource.EventCapacityUpdated_Title),
                    nameof(NotificationsResource.EventCapacityUpdated_Body),
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
