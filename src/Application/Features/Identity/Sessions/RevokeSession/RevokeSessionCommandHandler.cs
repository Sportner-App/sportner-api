using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Sessions.RevokeSession;

internal sealed class RevokeSessionCommandHandler : ICommandHandler<RevokeSessionCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RevokeSessionCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(SessionErrors.NotAuthenticated);
        }

        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.SessionId && candidate.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            return Result.Failure(SessionErrors.NotFound);
        }

        // Revoke is idempotent, so revoking an already-revoked session simply succeeds.
        session.Revoke(_timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
