using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.UserProfiles;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Auth.Register;

internal sealed class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, AuthenticationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public RegisterCommandHandler(
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
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var username = ProfileQueries.NormalizeUsername(request.Username);

        var usernameTaken = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(profile => profile.Username == username, cancellationToken);

        if (usernameTaken)
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.UsernameTaken);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.RegisterWithPassword(passwordHash, utcNow);
        var profile = UserProfile.Create(
            user.Id,
            username,
            request.FirstName.Trim(),
            utcNow,
            string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim());

        user.AttachUserProfile(profile);

        _dbContext.Users.Add(user);
        AddDefaultNotificationSettings(user.Id, utcNow);

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
                IsNewUser: true,
                IsOnboardingCompleted: user.HasCompletedOnboarding()));
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
