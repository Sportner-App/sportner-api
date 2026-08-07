using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.UpdateEventDetails;

public sealed record UpdateEventDetailsCommand(Guid EventId, string Title, string? Description)
    : ICommand<EventResponse>;

public sealed class UpdateEventDetailsCommandValidator : AbstractValidator<UpdateEventDetailsCommand>
{
    public UpdateEventDetailsCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
    }
}

internal sealed class UpdateEventDetailsCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<UpdateEventDetailsCommand, EventResponse>
{
    public UpdateEventDetailsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventDetailsCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            (@event, utcNow) => @event.UpdateDetails(request.Title, request.Description, utcNow),
            cancellationToken);
}
