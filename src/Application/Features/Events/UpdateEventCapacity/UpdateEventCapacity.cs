using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

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
    public UpdateEventCapacityCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventCapacityCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            (@event, utcNow) => @event.UpdateCapacity(request.MaxParticipants, utcNow),
            cancellationToken);
}
