using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Sessions.ListMySessions;

internal sealed class ListMySessionsQueryHandler
    : IQueryHandler<ListMySessionsQuery, IReadOnlyList<SessionResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListMySessionsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<SessionResponse>>> Handle(
        ListMySessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<SessionResponse>>.Failure(SessionErrors.NotAuthenticated);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var sessions = await _dbContext.UserSessions.AsNoTracking()
            .Where(session =>
                session.UserId == userId
                && session.RevokedAt == null
                && session.ExpiresAt > utcNow)
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new SessionResponse(
                session.Id,
                session.DeviceId,
                session.IpAddress,
                session.UserAgent,
                session.ExpiresAt,
                session.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SessionResponse>>.Success(sessions);
    }
}
