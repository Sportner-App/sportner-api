using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Email;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.Auth.ForgotPassword;

/// <summary>
/// Always succeeds from the caller's point of view, whether or not the email belongs to an
/// account — this is the standard defense against using "forgot password" to enumerate valid
/// accounts. A reset code is only actually issued/sent when a matching, password-capable,
/// reachable account exists and isn't in its resend cooldown.
/// </summary>
internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext dbContext,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null
            || user.Status != UserStatus.Active
            || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();

        if (!user.CanResendPasswordResetCode(utcNow, PasswordResetCodes.ResendCooldown))
        {
            return Result.Success();
        }

        var code = PasswordResetCodes.Generate();
        user.IssuePasswordResetCode(
            _tokenHasher.Hash(code),
            utcNow.Add(PasswordResetCodes.CodeLifetime),
            utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Best-effort: a delivery failure here shouldn't change the response — the endpoint
        // always looks the same from the outside regardless of what happened server-side.
        await _emailSender.SendPasswordResetCodeAsync(email, code, cancellationToken);

        return Result.Success();
    }
}
