using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Auth.VerifyOtp;

internal sealed class VerifyOtpCommandHandler
    : ICommandHandler<VerifyOtpCommand, AuthenticationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IJwtService _jwtService;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public VerifyOtpCommandHandler(
        IApplicationDbContext dbContext,
        IOtpService otpService,
        IJwtService jwtService,
        ITokenHasher tokenHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _jwtService = jwtService;
        _tokenHasher = tokenHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();

        var isValidOtp = await _otpService.VerifyAsync(phoneNumber, request.Code, cancellationToken);

        if (!isValidOtp)
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.InvalidOtp);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.PhoneNumber == phoneNumber, cancellationToken);

        var isNewUser = user is null;

        if (user is null)
        {
            user = User.Create(phoneNumber, utcNow);
            user.VerifyPhoneNumber(utcNow);
            user.Activate(utcNow);

            _dbContext.Users.Add(user);
            AddDefaultNotificationSettings(user.Id, utcNow);
        }
        else
        {
            if (user.Status is UserStatus.Banned or UserStatus.Deleted or UserStatus.Suspended)
            {
                return Result<AuthenticationResponse>.Failure(AuthErrors.AccountNotAccessible);
            }

            if (user.PhoneVerifiedAt is null)
            {
                user.VerifyPhoneNumber(utcNow);
            }

            if (user.Status is UserStatus.PendingVerification)
            {
                user.Activate(utcNow);
            }
        }

        var accessToken = _jwtService.CreateAccessToken(user.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _tokenHasher.Hash(refreshToken.Token);

        user.CreateSession(
            refreshTokenHash,
            refreshToken.ExpiresAt,
            utcNow,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent);

        user.UpdateLastLogin(utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new AuthenticationResponse(
            user.Id,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            isNewUser);

        return Result<AuthenticationResponse>.Success(response);
    }

    private void AddDefaultNotificationSettings(Guid userId, DateTimeOffset utcNow)
    {
        foreach (var notificationType in Enum.GetValues<NotificationType>())
        {
            _dbContext.NotificationSettings.Add(
                NotificationSetting.CreateDefault(userId, notificationType, utcNow));
        }
    }
}
