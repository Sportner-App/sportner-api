using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.ListWaitlist;

public sealed record ListWaitlistQuery(Guid EventId) : IQuery<IReadOnlyList<WaitlistEntryResponse>>;

internal sealed class ListWaitlistQueryHandler
    : IQueryHandler<ListWaitlistQuery, IReadOnlyList<WaitlistEntryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListWaitlistQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<WaitlistEntryResponse>>> Handle(
        ListWaitlistQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<WaitlistEntryResponse>>.Failure(EventErrors.NotAuthenticated);
        }

        var @event = await _dbContext.Events.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);

        if (@event is null)
        {
            return Result<IReadOnlyList<WaitlistEntryResponse>>.Failure(EventErrors.NotFound);
        }

        if (@event.OrganizerUserId != userId)
        {
            return Result<IReadOnlyList<WaitlistEntryResponse>>.Failure(EventErrors.NotOrganizer);
        }

        var items = await (
                from entry in _dbContext.EventWaitlists.AsNoTracking()
                join profile in _dbContext.UserProfiles.AsNoTracking()
                    on entry.UserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                where entry.EventId == request.EventId
                orderby entry.Position
                select new WaitlistEntryResponse(
                    entry.UserId,
                    profile != null ? profile.Username : null,
                    profile != null ? profile.FirstName : null,
                    profile != null ? profile.LastName : null,
                    entry.Position,
                    entry.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WaitlistEntryResponse>>.Success(items);
    }
}
