using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.PendingJoinRequests;

/// <summary>
/// Upcoming events I organize that still have applications waiting for my approval. Drives the
/// home-screen card so the organizer notices requests even if they missed the push.
/// </summary>
public sealed record ListPendingJoinRequestEventsQuery
    : IQuery<IReadOnlyList<PendingJoinRequestEventResponse>>;

internal sealed class ListPendingJoinRequestEventsQueryHandler
    : IQueryHandler<ListPendingJoinRequestEventsQuery, IReadOnlyList<PendingJoinRequestEventResponse>>
{
    // The card only shows a few faces; the count carries the rest.
    private const int ApplicantPreviewLimit = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListPendingJoinRequestEventsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<PendingJoinRequestEventResponse>>> Handle(
        ListPendingJoinRequestEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<PendingJoinRequestEventResponse>>.Failure(
                EventErrors.NotAuthenticated);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var events = await _dbContext.Events.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizerUserId == userId
                && (candidate.Status == EventStatus.Published || candidate.Status == EventStatus.Full)
                && candidate.EventDate > utcNow)
            .OrderBy(candidate => candidate.EventDate)
            .Select(candidate => new { candidate.Id, candidate.Title, candidate.EventDate })
            .ToListAsync(cancellationToken);

        var eventIds = events.Select(candidate => candidate.Id).ToList();

        // Flat queries + in-memory join, same as ListPendingAttendanceEvents (InMemory provider).
        var pendingParticipants = await _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                eventIds.Contains(participant.EventId)
                && participant.UserId != null
                && participant.Status == ParticipantStatus.Pending)
            .Select(participant => new
            {
                participant.EventId,
                UserId = participant.UserId!.Value,
                participant.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (pendingParticipants.Count == 0)
        {
            return Result<IReadOnlyList<PendingJoinRequestEventResponse>>.Success([]);
        }

        var applicantUserIds = pendingParticipants
            .Select(participant => participant.UserId)
            .Distinct()
            .ToList();

        var profilesByUserId = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => applicantUserIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        var items = new List<PendingJoinRequestEventResponse>();

        foreach (var @event in events)
        {
            var eventApplicants = pendingParticipants
                .Where(participant => participant.EventId == @event.Id)
                .OrderByDescending(participant => participant.CreatedAt)
                .ToList();

            if (eventApplicants.Count == 0)
            {
                continue;
            }

            var preview = eventApplicants
                .Take(ApplicantPreviewLimit)
                .Select(participant =>
                {
                    profilesByUserId.TryGetValue(participant.UserId, out var profile);
                    return new PendingJoinRequestApplicantResponse(
                        participant.UserId,
                        profile?.Username,
                        profile?.FirstName,
                        profile?.ProfileImageUrl);
                })
                .ToList();

            items.Add(new PendingJoinRequestEventResponse(
                @event.Id, @event.Title, @event.EventDate, eventApplicants.Count, preview));
        }

        return Result<IReadOnlyList<PendingJoinRequestEventResponse>>.Success(items);
    }
}
