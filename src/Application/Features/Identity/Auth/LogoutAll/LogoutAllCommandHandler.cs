using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.LogoutAll;

internal sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public LogoutAllCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var activeSessions = await _dbContext.UserSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions.Where(session => session.IsActive(utcNow)))
        {
            session.Revoke(utcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
