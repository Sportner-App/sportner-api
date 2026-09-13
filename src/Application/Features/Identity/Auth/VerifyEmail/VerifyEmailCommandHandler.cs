using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.VerifyEmail;

internal sealed class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public VerifyEmailCommandHandler(
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

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        if (user.EmailVerifiedAt is not null)
        {
            return Result.Failure(AuthErrors.EmailAlreadyVerified);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Failure(AuthErrors.EmailRequired);
        }

        var utcNow = _timeProvider.GetUtcNow();

        if (user.EmailVerificationCodeHash is null || user.EmailVerificationCodeExpiresAt is null)
        {
            return Result.Failure(AuthErrors.EmailVerificationCodeInvalid);
        }

        if (utcNow > user.EmailVerificationCodeExpiresAt)
        {
            return Result.Failure(AuthErrors.EmailVerificationCodeExpired);
        }

        if (!_tokenHasher.Verify(request.Code, user.EmailVerificationCodeHash))
        {
            return Result.Failure(AuthErrors.EmailVerificationCodeInvalid);
        }

        user.ConfirmEmailVerified(utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
