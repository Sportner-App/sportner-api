using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.UpdateEventFee;

public sealed record UpdateEventFeeCommand(Guid EventId, bool IsPaid, decimal? FeeAmount)
    : ICommand<EventResponse>;

public sealed class UpdateEventFeeCommandValidator : AbstractValidator<UpdateEventFeeCommand>
{
    public UpdateEventFeeCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.FeeAmount)
            .NotNull()
            .GreaterThan(0)
            .LessThanOrEqualTo(Event.MaxFeeAmount)
            .When(command => command.IsPaid)
            .WithMessage("Fee amount is required and must be greater than zero for paid events.");
    }
}

internal sealed class UpdateEventFeeCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<UpdateEventFeeCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public UpdateEventFeeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventFeeCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.UpdateFee(request.IsPaid, request.FeeAmount, utcNow);

                await EventRosterNotifier.NotifyParticipantsAsync(
                    DbContext,
                    _notificationPublisher,
                    @event,
                    NotificationType.EventFeeUpdated,
                    nameof(NotificationsResource.EventFeeUpdated_Title),
                    nameof(NotificationsResource.EventFeeUpdated_Body),
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
