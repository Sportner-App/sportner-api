using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.UserProfiles;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Auth.Login;

internal sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, AuthenticationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ITokenHasher tokenHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _tokenHasher = tokenHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var username = ProfileQueries.NormalizeUsername(request.Username);

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);

        if (profile is null)
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == profile.UserId, cancellationToken);

        if (user is null
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (user.Status is UserStatus.Banned or UserStatus.Deleted or UserStatus.Suspended
            || !user.CanAuthenticate())
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.AccountNotAccessible);
        }

        var utcNow = _timeProvider.GetUtcNow();
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
                IsOnboardingCompleted: user.HasCompletedOnboarding()));
    }
}
