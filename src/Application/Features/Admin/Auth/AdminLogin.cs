using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Email;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.Auth;
using Sportner.Domain.Admin;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Admin.Auth;

/// <summary>
/// Paneldeki giriş, şifre ya da Google kurulumu gerektirmesin diye e-postaya giden tek
/// kullanımlık kodla yapılır. Yanıt, adresin bir yöneticiye ait olup olmadığını hiçbir
/// zaman belli etmez: yönetici değilse de aynı başarılı yanıt döner, sadece mail gitmez.
/// </summary>
public sealed record RequestAdminLoginCodeCommand(string Email) : ICommand;

internal sealed class RequestAdminLoginCodeCommandHandler : ICommandHandler<RequestAdminLoginCodeCommand>
{
    internal static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    private readonly IApplicationDbContext _dbContext;
    private readonly IAdminDirectory _admins;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RequestAdminLoginCodeCommandHandler> _logger;

    public RequestAdminLoginCodeCommandHandler(
        IApplicationDbContext dbContext,
        IAdminDirectory admins,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<RequestAdminLoginCodeCommandHandler> logger)
    {
        _dbContext = dbContext;
        _admins = admins;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestAdminLoginCodeCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || user.Status != UserStatus.Active || !_admins.IsAdmin(user.Id))
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        var since = utcNow - Cooldown;
        if (await _dbContext.AdminLoginCodes.AnyAsync(
                code => code.UserId == user.Id && code.CreatedAt >= since, cancellationToken))
        {
            return Result.Failure(AdminErrors.CodeTooFrequent);
        }

        var plain = EmailVerificationCodes.Generate();
        _dbContext.AdminLoginCodes.Add(
            AdminLoginCode.Create(user.Id, _tokenHasher.Hash(plain), utcNow + CodeLifetime, utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sent = await _emailSender.SendAdminLoginCodeAsync(email, plain, cancellationToken);
        if (!sent.Succeeded)
        {
            _logger.LogWarning("Admin login code email failed: {Error}", sent.ErrorMessage);
            return Result.Failure(AdminErrors.EmailFailed);
        }

        return Result.Success();
    }
}

public sealed record VerifyAdminLoginCodeCommand(string Email, string Code, string? IpAddress, string? UserAgent)
    : ICommand<AuthenticationResponse>;

internal sealed class VerifyAdminLoginCodeCommandHandler
    : ICommandHandler<VerifyAdminLoginCodeCommand, AuthenticationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAdminDirectory _admins;
    private readonly ITokenHasher _tokenHasher;
    private readonly IJwtService _jwtService;
    private readonly TimeProvider _timeProvider;

    public VerifyAdminLoginCodeCommandHandler(
        IApplicationDbContext dbContext,
        IAdminDirectory admins,
        ITokenHasher tokenHasher,
        IJwtService jwtService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _admins = admins;
        _tokenHasher = tokenHasher;
        _jwtService = jwtService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        VerifyAdminLoginCodeCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || user.Status != UserStatus.Active || !_admins.IsAdmin(user.Id))
        {
            return Result<AuthenticationResponse>.Failure(AdminErrors.InvalidCode);
        }

        var utcNow = _timeProvider.GetUtcNow();

        // Yalnızca en son istenen kod geçerli; eski kodlar yeni istekle hükümsüz kalır.
        var code = await _dbContext.AdminLoginCodes
            .Where(candidate => candidate.UserId == user.Id)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (code is null || !code.IsUsable(utcNow))
        {
            return Result<AuthenticationResponse>.Failure(AdminErrors.InvalidCode);
        }

        if (!_tokenHasher.Verify(request.Code.Trim(), code.CodeHash))
        {
            code.RegisterFailure(utcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<AuthenticationResponse>.Failure(AdminErrors.InvalidCode);
        }

        code.Consume(utcNow);

        var accessToken = _jwtService.CreateAccessToken(user.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();
        _dbContext.UserSessions.Add(
            UserSession.Create(
                user.Id,
                _tokenHasher.Hash(refreshToken.Token),
                refreshToken.ExpiresAt,
                utcNow,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent));
        user.UpdateLastLogin(utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthenticationResponse>.Success(
            new AuthenticationResponse(
                user.Id,
                accessToken.Token,
                accessToken.ExpiresAt,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                IsNewUser: false,
                IsOnboardingCompleted: user.HasCompletedOnboarding(),
                IsEmailVerified: user.EmailVerifiedAt is not null));
    }
}
