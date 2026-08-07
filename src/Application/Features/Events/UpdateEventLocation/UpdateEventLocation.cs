using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

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
    public UpdateEventLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventLocationCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            (@event, utcNow) =>
                @event.UpdateLocation(request.Latitude, request.Longitude, request.Address, utcNow),
            cancellationToken);
}
