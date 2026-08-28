using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.AcceptEventInvitation;

public sealed record AcceptEventInvitationCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class AcceptEventInvitationCommandHandler
    : ICommandHandler<AcceptEventInvitationCommand, EventResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AcceptEventInvitationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<EventResponse>> Handle(
        AcceptEventInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<EventResponse>.Failure(EventErrors.NotAuthenticated);
        }

        var @event = await EventAccess.LoadAggregateAsync(_dbContext, request.EventId, cancellationToken);
        if (@event is null)
        {
            return Result<EventResponse>.Failure(EventErrors.NotFound);
        }

        var participant = @event.Participants.FirstOrDefault(item => item.UserId == userId);
        if (participant?.Status is not Domain.Common.Enums.ParticipantStatus.Invited)
        {
            return Result<EventResponse>.Failure(EventErrors.InvitationNotFound);
        }

        if (!@event.HasAvailableCapacity())
        {
            return Result<EventResponse>.Failure(EventErrors.CapacityFull);
        }

        var utcNow = _timeProvider.GetUtcNow();
        @event.AcceptInvitation(userId, utcNow);
        await EventAccess.AddConversationMemberIfPresentAsync(
            _dbContext, @event.Id, userId, utcNow, cancellationToken);

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        statistics?.IncreaseEventsJoined(utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);
        var response = await EventQueries.GetDetailAsync(
            _dbContext, @event.Id, userId, cancellationToken);
        return Result<EventResponse>.Success(response!);
    }
}
