using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.ResetPassword;

internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public ResetPasswordCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        // Every failure path below returns the same generic error — whether the account doesn't
        // exist, never requested a code, or the code is wrong/expired is never distinguishable
        // from the response (account enumeration defense, same as ForgotPasswordCommandHandler).
        if (user is null
            || user.PasswordResetCodeHash is null
            || user.PasswordResetCodeExpiresAt is null)
        {
            return Result.Failure(AuthErrors.PasswordResetCodeInvalid);
        }

        var utcNow = _timeProvider.GetUtcNow();

        if (utcNow > user.PasswordResetCodeExpiresAt)
        {
            return Result.Failure(AuthErrors.PasswordResetCodeInvalid);
        }

        if (!_tokenHasher.Verify(request.Code, user.PasswordResetCodeHash))
        {
            return Result.Failure(AuthErrors.PasswordResetCodeInvalid);
        }

        user.ResetPassword(_passwordHasher.Hash(request.NewPassword), utcNow);

        // A password reset is a strong signal the account may have been compromised — kick every
        // other active session out, mirroring LogoutAllCommandHandler.
        var activeSessions = await _dbContext.UserSessions
            .Where(session => session.UserId == user.Id && session.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions.Where(session => session.IsActive(utcNow)))
        {
            session.Revoke(utcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
