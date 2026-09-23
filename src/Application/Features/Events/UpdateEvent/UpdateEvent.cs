using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.UpdateEvent;

public sealed record UpdateEventCommand(
    Guid EventId,
    string Title,
    string? Description,
    DateTimeOffset EventDate,
    int DurationMinutes,
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? MaxParticipants,
    bool IsPaid,
    decimal? FeeAmount) : ICommand<EventResponse>;

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.DurationMinutes).GreaterThan(0);
        RuleFor(command => command.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(command => command.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(command => command.Address).NotEmpty();
        RuleFor(command => command.MaxParticipants).GreaterThan(0).When(command => command.MaxParticipants is not null);
        RuleFor(command => command.FeeAmount)
            .NotNull().GreaterThan(0).LessThanOrEqualTo(Event.MaxFeeAmount)
            .When(command => command.IsPaid);
    }
}

internal sealed class UpdateEventCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<UpdateEventCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public UpdateEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(UpdateEventCommand request, CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                var changes = new List<EventUpdateField>();
                var normalizedTitle = request.Title.Trim();
                var normalizedDescription = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
                var normalizedAddress = request.Address.Trim();

                if (@event.Title != normalizedTitle || @event.Description != normalizedDescription)
                {
                    @event.UpdateDetails(normalizedTitle, normalizedDescription, utcNow);
                    changes.Add(EventUpdateField.Details);
                }

                if (@event.EventDate != request.EventDate || @event.DurationMinutes != request.DurationMinutes)
                {
                    @event.UpdateSchedule(request.EventDate, request.DurationMinutes, utcNow);
                    changes.Add(EventUpdateField.Schedule);
                }

                if (@event.Latitude != request.Latitude || @event.Longitude != request.Longitude || @event.Address != normalizedAddress)
                {
                    @event.UpdateLocation(request.Latitude, request.Longitude, normalizedAddress, utcNow);
                    changes.Add(EventUpdateField.Location);
                }

                if (@event.MaxParticipants != request.MaxParticipants)
                {
                    @event.UpdateCapacity(request.MaxParticipants, utcNow);
                    changes.Add(EventUpdateField.Capacity);
                }

                if (@event.IsPaid != request.IsPaid || @event.FeeAmount != request.FeeAmount)
                {
                    @event.UpdateFee(request.IsPaid, request.FeeAmount, utcNow);
                    changes.Add(EventUpdateField.Fee);
                }

                if (changes.Count > 0)
                {
                    await EventRosterNotifier.NotifyApprovedParticipantsOfUpdateAsync(
                        DbContext, _notificationPublisher, @event, changes, ct);
                }

                return Result.Success();
            },
            cancellationToken);
}
