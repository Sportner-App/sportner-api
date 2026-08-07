using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

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
    public UpdateEventScheduleCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventScheduleCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            (@event, utcNow) => @event.UpdateSchedule(request.EventDate, request.DurationMinutes, utcNow),
            cancellationToken);
}
