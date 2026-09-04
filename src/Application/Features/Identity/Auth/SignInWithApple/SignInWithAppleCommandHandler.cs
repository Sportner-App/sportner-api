using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Auth.SignInWithApple;

internal sealed class SignInWithAppleCommandHandler
    : ICommandHandler<SignInWithAppleCommand, ExternalSignInResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppleTokenVerifier _tokenVerifier;
    private readonly IJwtService _jwtService;
    private readonly ITokenHasher _tokenHasher;
    private readonly IExternalRegistrationTokenService _registrationTokenService;
    private readonly TimeProvider _timeProvider;

    public SignInWithAppleCommandHandler(
        IApplicationDbContext dbContext,
        IAppleTokenVerifier tokenVerifier,
        IJwtService jwtService,
        ITokenHasher tokenHasher,
        IExternalRegistrationTokenService registrationTokenService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tokenVerifier = tokenVerifier;
        _jwtService = jwtService;
        _tokenHasher = tokenHasher;
        _registrationTokenService = registrationTokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ExternalSignInResponse>> Handle(
        SignInWithAppleCommand request,
        CancellationToken cancellationToken)
    {
        var verification = await _tokenVerifier.VerifyAsync(request.IdentityToken, cancellationToken);

        if (verification.IsFailure)
        {
            return Result<ExternalSignInResponse>.Failure(AuthErrors.ExternalTokenInvalid);
        }

        var identity = verification.Value!;
        var utcNow = _timeProvider.GetUtcNow();

        var externalLogin = await _dbContext.UserExternalLogins.AsNoTracking()
            .FirstOrDefaultAsync(
                login => login.Provider == ExternalLoginProvider.Apple
                    && login.ProviderUserId == identity.ProviderUserId,
                cancellationToken);

        if (externalLogin is null)
        {
            var ticketData = new ExternalRegistrationTicket(
                ExternalLoginProvider.Apple,
                identity.ProviderUserId,
                identity.Email,
                request.FirstName,
                request.LastName,
                null);
            var ticket = _registrationTokenService.Create(ticketData);
            var suggestedUsername = await ExternalAuthQueries.SuggestUsernameAsync(
                _dbContext,
                request.FirstName,
                request.LastName,
                identity.ProviderUserId,
                cancellationToken);

            return Result<ExternalSignInResponse>.Success(
                ExternalSignInResponse.RegistrationRequired(ticket, suggestedUsername, ticketData));
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.ExternalLogins)
            .FirstOrDefaultAsync(candidate => candidate.Id == externalLogin.UserId, cancellationToken);

        if (user is null
            || user.Status is UserStatus.Banned or UserStatus.Deleted or UserStatus.Suspended
            || !user.CanAuthenticate())
        {
            return Result<ExternalSignInResponse>.Failure(AuthErrors.AccountNotAccessible);
        }

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

        return Result<ExternalSignInResponse>.Success(
            ExternalSignInResponse.SignedIn(new AuthenticationResponse(
                user.Id,
                accessToken.Token,
                accessToken.ExpiresAt,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                IsNewUser: false,
                IsOnboardingCompleted: user.HasCompletedOnboarding())));
    }
}
