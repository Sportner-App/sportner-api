using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.UserProfiles;
using Sportner.Domain.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Auth.CompleteExternalRegistration;

internal sealed class CompleteExternalRegistrationCommandHandler
    : ICommandHandler<CompleteExternalRegistrationCommand, AuthenticationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IExternalRegistrationTokenService _registrationTokenService;
    private readonly IJwtService _jwtService;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public CompleteExternalRegistrationCommandHandler(
        IApplicationDbContext dbContext,
        IExternalRegistrationTokenService registrationTokenService,
        IJwtService jwtService,
        ITokenHasher tokenHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _registrationTokenService = registrationTokenService;
        _jwtService = jwtService;
        _tokenHasher = tokenHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        CompleteExternalRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await _registrationTokenService.ValidateAsync(
            request.RegistrationToken,
            cancellationToken);
        if (validation.IsFailure)
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.ExternalRegistrationTokenInvalid);
        }

        var ticket = validation.Value!;
        var alreadyRegistered = await _dbContext.UserExternalLogins.AsNoTracking()
            .AnyAsync(login => login.Provider == ticket.Provider
                && login.ProviderUserId == ticket.ProviderUserId, cancellationToken);
        if (alreadyRegistered)
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.ExternalLoginAlreadyRegistered);
        }

        var username = ProfileQueries.NormalizeUsername(request.Username);
        if (await _dbContext.UserProfiles.AsNoTracking()
            .AnyAsync(profile => profile.Username == username, cancellationToken))
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.UsernameTaken);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var user = User.RegisterWithExternalProvider(
            ticket.Provider,
            ticket.ProviderUserId,
            ticket.Email,
            utcNow);
        var profile = UserProfile.Create(
            user.Id,
            username,
            request.FirstName.Trim(),
            utcNow,
            string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim());
        profile.UpdatePersonalDetails(request.Gender, request.BirthDate, utcNow);
        if (!string.IsNullOrWhiteSpace(ticket.ProfileImageUrl))
        {
            profile.UpdateAvatar(ticket.ProfileImageUrl, utcNow);
        }
        user.AttachUserProfile(profile);

        _dbContext.Users.Add(user);
        _dbContext.MarkAsAdded(user.Statistics!);
        _dbContext.MarkAsAdded(user.ExternalLogins.Single());
        _dbContext.MarkAsAdded(profile);
        foreach (var notificationType in Enum.GetValues<NotificationType>())
        {
            _dbContext.NotificationSettings.Add(
                NotificationSetting.CreateDefault(user.Id, notificationType, utcNow));
        }

        var accessToken = _jwtService.CreateAccessToken(user.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();
        _dbContext.UserSessions.Add(UserSession.Create(
            user.Id,
            _tokenHasher.Hash(refreshToken.Token),
            refreshToken.ExpiresAt,
            utcNow,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent));
        user.UpdateLastLogin(utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthenticationResponse>.Success(new AuthenticationResponse(
            user.Id,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            IsNewUser: true,
            IsOnboardingCompleted: false));
    }
}
