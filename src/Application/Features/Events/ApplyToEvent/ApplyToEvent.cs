using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Organizations;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ApplyToEvent;

public sealed record ApplyToEventCommand(Guid EventId) : ICommand<ApplyToEventResponse>;

internal sealed class ApplyToEventCommandHandler
    : ICommandHandler<ApplyToEventCommand, ApplyToEventResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ApplyToEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ApplyToEventResponse>> Handle(
        ApplyToEventCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.UserNotFound);
        }

        if (!user.CanCreateContent())
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.CannotCreateContent);
        }

        var @event = await EventAccess.LoadAggregateAsync(_dbContext, request.EventId, cancellationToken);

        if (@event is null)
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.NotFound);
        }

        if (@event.OrganizationId is { } organizationId
            && !await OrganizationQueries.IsApprovedMemberAsync(
                _dbContext,
                organizationId,
                userId,
                cancellationToken))
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.NotOrganizationMember);
        }

        if (@event.OrganizerUserId == userId)
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.OrganizerCannotApply);
        }

        if (await BlockQueries.BlockedPairExistsAsync(
                _dbContext,
                userId,
                @event.OrganizerUserId,
                cancellationToken))
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.RelationshipBlocked);
        }

        if (@event.Status is not (EventStatus.Published or EventStatus.Full))
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.NotAcceptingApplications);
        }

        var existing = @event.Participants.FirstOrDefault(participant => participant.UserId == userId);

        if ((existing is not null && existing.Status is not ParticipantStatus.Cancelled)
            || @event.Waitlist.Any(entry => entry.UserId == userId))
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.AlreadyApplied);
        }

        var birthDate = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.BirthDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (birthDate is null || !@event.IsParticipantAgeEligible(birthDate.Value))
        {
            return Result<ApplyToEventResponse>.Failure(EventErrors.ParticipantAgeNotEligible);
        }

        var (participant, waitlistEntry) = @event.Apply(userId, _timeProvider.GetUtcNow());

        // Client-generated Guids can be tracked as Modified by EF; force insert for new rows.
        if (participant is not null && existing is null)
        {
            _dbContext.MarkAsAdded(participant);
        }

        if (waitlistEntry is not null)
        {
            _dbContext.MarkAsAdded(waitlistEntry);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ApplyToEventResponse>.Success(new ApplyToEventResponse(
            JoinedWaitlist: waitlistEntry is not null,
            ParticipantStatus: participant is null ? null : (short)participant.Status,
            WaitlistPosition: waitlistEntry?.Position));
    }
}
