using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ListParticipants;

public sealed record ListParticipantsQuery(Guid EventId) : IQuery<IReadOnlyList<ParticipantResponse>>;

internal sealed class ListParticipantsQueryHandler
    : IQueryHandler<ListParticipantsQuery, IReadOnlyList<ParticipantResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListParticipantsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ParticipantResponse>>> Handle(
        ListParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await _dbContext.Events.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);

        if (@event is null)
        {
            return Result<IReadOnlyList<ParticipantResponse>>.Failure(EventErrors.NotFound);
        }

        var isOrganizer = _currentUser.UserId == @event.OrganizerUserId;

        // Cancelled / rejected are history — they are not current participants.
        var query =
            from participant in _dbContext.EventParticipants.AsNoTracking()
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on participant.UserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            where participant.EventId == request.EventId
                && participant.Status != ParticipantStatus.Cancelled
                && participant.Status != ParticipantStatus.Rejected
            select new { participant, profile };

        if (!isOrganizer)
        {
            // Pending applicants are not roster members until the organizer approves.
            query = query.Where(row =>
                row.participant.Status == ParticipantStatus.Approved
                || row.participant.Status == ParticipantStatus.Attended
                || row.participant.Status == ParticipantStatus.NoShow);
        }

        var items = await query
            .OrderBy(row => row.participant.CreatedAt)
            .Select(row => new ParticipantResponse(
                row.participant.Id,
                row.participant.UserId,
                (short)row.participant.Kind,
                row.participant.Kind == ParticipantKind.Guest,
                row.profile != null ? row.profile.Username : null,
                row.participant.Kind == ParticipantKind.Guest
                    ? row.participant.GuestFirstName
                    : row.profile != null ? row.profile.FirstName : null,
                row.participant.Kind == ParticipantKind.Guest
                    ? row.participant.GuestLastName
                    : row.profile != null ? row.profile.LastName : null,
                row.profile != null ? row.profile.ProfileImageUrl : null,
                (short)row.participant.Status,
                row.participant.JoinedAt,
                row.participant.AttendedAt,
                row.participant.CanReview))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ParticipantResponse>>.Success(items);
    }
}
