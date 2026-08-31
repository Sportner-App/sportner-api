using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Events;

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
    public UpdateEventFeeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        UpdateEventFeeCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            (@event, utcNow) => @event.UpdateFee(request.IsPaid, request.FeeAmount, utcNow),
            cancellationToken);
}
