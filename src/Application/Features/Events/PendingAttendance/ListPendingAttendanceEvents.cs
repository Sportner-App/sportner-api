using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.PendingAttendance;

/// <summary>
/// Events I organized that ended but still have Approved participants whose attendance was
/// never taken — nobody involved can review anyone until that happens. Drives the app-launch
/// prompt nudging the organizer to close it out (see docs/features/05-reviews.md).
/// </summary>
public sealed record ListPendingAttendanceEventsQuery : IQuery<IReadOnlyList<PendingAttendanceEventResponse>>;

internal sealed class ListPendingAttendanceEventsQueryHandler
    : IQueryHandler<ListPendingAttendanceEventsQuery, IReadOnlyList<PendingAttendanceEventResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListPendingAttendanceEventsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PendingAttendanceEventResponse>>> Handle(
        ListPendingAttendanceEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<PendingAttendanceEventResponse>>.Failure(
                EventErrors.NotAuthenticated);
        }

        var pendingEventIds = await _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.UserId != null && participant.Status == ParticipantStatus.Approved)
            .Select(participant => participant.EventId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var events = await _dbContext.Events.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizerUserId == userId
                && candidate.Status == EventStatus.Completed
                && pendingEventIds.Contains(candidate.Id))
            .OrderBy(candidate => candidate.EventDate)
            .Select(candidate => new { candidate.Id, candidate.Title, candidate.EventDate })
            .ToListAsync(cancellationToken);

        var eventIds = events.Select(candidate => candidate.Id).ToList();

        // Fetched as two flat queries + an in-memory join, rather than a SQL-style LEFT JOIN —
        // EF Core's InMemory provider (used in tests) has well-known gaps translating
        // GroupJoin/DefaultIfEmpty reliably; this shape works identically against Npgsql too.
        var pendingParticipants = await _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                eventIds.Contains(participant.EventId)
                && participant.UserId != null
                && participant.Status == ParticipantStatus.Approved)
            .ToListAsync(cancellationToken);

        var participantUserIds = pendingParticipants
            .Select(participant => participant.UserId!.Value)
            .Distinct()
            .ToList();

        var profilesByUserId = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => participantUserIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        var items = new List<PendingAttendanceEventResponse>(events.Count);

        foreach (var @event in events)
        {
            var participants = pendingParticipants
                .Where(participant => participant.EventId == @event.Id)
                .Select(participant =>
                {
                    profilesByUserId.TryGetValue(participant.UserId!.Value, out var profile);
                    return new PendingAttendanceParticipantResponse(
                        participant.UserId!.Value,
                        profile?.Username,
                        profile?.FirstName,
                        profile?.ProfileImageUrl);
                })
                .OrderBy(participant => participant.Username ?? participant.UserId.ToString())
                .ToList();

            if (participants.Count == 0)
            {
                continue;
            }

            items.Add(new PendingAttendanceEventResponse(
                @event.Id, @event.Title, @event.EventDate, participants));
        }

        return Result<IReadOnlyList<PendingAttendanceEventResponse>>.Success(items);
    }
}
