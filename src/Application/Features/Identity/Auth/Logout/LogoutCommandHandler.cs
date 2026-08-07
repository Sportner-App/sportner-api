using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.Logout;

internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public LogoutCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ITokenHasher tokenHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _tokenHasher = tokenHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var tokenHash = _tokenHasher.Hash(request.RefreshToken);

        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                candidate => candidate.RefreshTokenHash == tokenHash && candidate.UserId == userId,
                cancellationToken);

        // Idempotent: unknown or already-revoked tokens still resolve to success.
        if (session is not null && session.IsActive(utcNow))
        {
            session.Revoke(utcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
