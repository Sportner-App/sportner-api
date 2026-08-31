using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.Features.Events.CreateRecurringEvents;

public sealed record CreateRecurringEventsCommand(
    Guid SportId,
    string Title,
    string? Description,
    DateTimeOffset EventDate,
    int DurationMinutes,
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? MaxParticipants,
    int MinParticipantAge,
    int MaxParticipantAge,
    int IntervalWeeks,
    int OccurrenceCount,
    bool IsPaid = false,
    decimal? FeeAmount = null) : ICommand<CreateRecurringEventsResponse>;

public sealed record CreateRecurringEventsResponse(
    Guid FirstEventId,
    IReadOnlyList<Guid> EventIds);

public sealed class CreateRecurringEventsCommandValidator
    : AbstractValidator<CreateRecurringEventsCommand>
{
    public CreateRecurringEventsCommandValidator()
    {
        RuleFor(x => x.SportId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.MaxParticipants).GreaterThan(0).When(x => x.MaxParticipants is not null);
        RuleFor(x => x.MinParticipantAge).InclusiveBetween(13, 120);
        RuleFor(x => x.MaxParticipantAge).InclusiveBetween(13, 120)
            .GreaterThanOrEqualTo(x => x.MinParticipantAge);
        RuleFor(x => x.IntervalWeeks).Must(value => value is 1 or 2 or 4);
        RuleFor(x => x.OccurrenceCount).InclusiveBetween(2, 12);
        RuleFor(x => x.FeeAmount)
            .NotNull()
            .GreaterThan(0)
            .LessThanOrEqualTo(DomainEvent.MaxFeeAmount)
            .When(x => x.IsPaid)
            .WithMessage("Fee amount is required and must be greater than zero for paid events.");
    }
}

internal sealed class CreateRecurringEventsCommandHandler
    : ICommandHandler<CreateRecurringEventsCommand, CreateRecurringEventsResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateRecurringEventsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateRecurringEventsResponse>> Handle(
        CreateRecurringEventsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return Result<CreateRecurringEventsResponse>.Failure(EventErrors.NotAuthenticated);

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return Result<CreateRecurringEventsResponse>.Failure(EventErrors.UserNotFound);
        if (!user.CanCreateContent())
            return Result<CreateRecurringEventsResponse>.Failure(EventErrors.CannotCreateContent);

        var sport = await _dbContext.Sports.FirstOrDefaultAsync(x => x.Id == request.SportId, cancellationToken);
        if (sport is null)
            return Result<CreateRecurringEventsResponse>.Failure(EventErrors.SportNotFound);
        if (!sport.CanBeUsed())
            return Result<CreateRecurringEventsResponse>.Failure(EventErrors.SportInactive);

        var utcNow = _timeProvider.GetUtcNow();
        var events = Enumerable.Range(0, request.OccurrenceCount)
            .Select(index => DomainEvent.Create(
                userId,
                request.SportId,
                request.Title,
                request.EventDate.AddDays(index * request.IntervalWeeks * 7),
                request.DurationMinutes,
                request.Latitude,
                request.Longitude,
                request.Address,
                utcNow,
                request.Description,
                request.MaxParticipants,
                request.MinParticipantAge,
                request.MaxParticipantAge,
                skillLevel: null,
                isPaid: request.IsPaid,
                feeAmount: request.FeeAmount))
            .ToList();

        foreach (var @event in events)
            _dbContext.Events.Add(@event);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<CreateRecurringEventsResponse>.Success(
            new CreateRecurringEventsResponse(events[0].Id, events.Select(x => x.Id).ToArray()));
    }
}
