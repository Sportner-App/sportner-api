using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.UpdateEventLocation;

public sealed record UpdateEventLocationCommand(
    Guid EventId,
    decimal Latitude,
    decimal Longitude,
    string Address) : ICommand<EventResponse>;

public sealed class UpdateEventLocationCommandValidator : AbstractValidator<UpdateEventLocationCommand>
{
    public UpdateEventLocationCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(command => command.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(command => command.Address).NotEmpty();
    }
}

internal sealed class UpdateEventLocationCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<UpdateEventLocationCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public UpdateEventLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventLocationCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.UpdateLocation(request.Latitude, request.Longitude, request.Address, utcNow);

                await EventRosterNotifier.NotifyParticipantsAsync(
                    DbContext,
                    _notificationPublisher,
                    @event,
                    NotificationType.EventLocationUpdated,
                    nameof(NotificationsResource.EventLocationUpdated_Title),
                    nameof(NotificationsResource.EventLocationUpdated_Body),
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
