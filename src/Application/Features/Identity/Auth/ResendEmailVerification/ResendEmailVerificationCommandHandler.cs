using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Email;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.ResendEmailVerification;

internal sealed class ResendEmailVerificationCommandHandler
    : ICommandHandler<ResendEmailVerificationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public ResendEmailVerificationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        ResendEmailVerificationCommand request,
        CancellationToken cancellationToken)
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

        if (!user.CanResendEmailVerificationCode(utcNow, EmailVerificationCodes.ResendCooldown))
        {
            return Result.Failure(AuthErrors.EmailVerificationCooldown);
        }

        var code = EmailVerificationCodes.Generate();
        user.IssueEmailVerificationCode(
            _tokenHasher.Hash(code),
            utcNow.Add(EmailVerificationCodes.CodeLifetime),
            utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailSender.SendVerificationCodeAsync(user.Email, code, cancellationToken);

        return Result.Success();
    }
}
