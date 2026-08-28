using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.Features.Events.CreateEvent;

internal sealed class CreateEventCommandHandler
    : ICommandHandler<CreateEventCommand, EventResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<EventResponse>> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<EventResponse>.Failure(EventErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<EventResponse>.Failure(EventErrors.UserNotFound);
        }

        if (!user.CanCreateContent())
        {
            return Result<EventResponse>.Failure(EventErrors.CannotCreateContent);
        }

        var sport = await _dbContext.Sports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SportId, cancellationToken);

        if (sport is null)
        {
            return Result<EventResponse>.Failure(EventErrors.SportNotFound);
        }

        if (!sport.CanBeUsed())
        {
            return Result<EventResponse>.Failure(EventErrors.SportInactive);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var @event = DomainEvent.Create(
            userId,
            request.SportId,
            request.Title,
            request.EventDate,
            request.DurationMinutes,
            request.Latitude,
            request.Longitude,
            request.Address,
            utcNow,
            request.Description,
            request.MaxParticipants,
            request.MinParticipantAge,
            request.MaxParticipantAge);

        _dbContext.Events.Add(@event);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await EventQueries.GetDetailAsync(_dbContext, @event.Id, userId, cancellationToken);

        return Result<EventResponse>.Success(response!);
    }
}
