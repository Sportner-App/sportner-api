using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, AuthenticationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly ITokenHasher _tokenHasher;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IApplicationDbContext dbContext,
        IJwtService jwtService,
        ITokenHasher tokenHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _tokenHasher = tokenHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthenticationResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var tokenHash = _tokenHasher.Hash(request.RefreshToken);

        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                candidate => candidate.RefreshTokenHash == tokenHash,
                cancellationToken);

        if (session is null || !session.IsActive(utcNow))
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == session.UserId, cancellationToken);

        if (user is null || !user.CanAuthenticate())
        {
            return Result<AuthenticationResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var accessToken = _jwtService.CreateAccessToken(user.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var newHash = _tokenHasher.Hash(refreshToken.Token);

        session.RotateRefreshToken(newHash, refreshToken.ExpiresAt, utcNow);
        user.UpdateLastLogin(utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new AuthenticationResponse(
            user.Id,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            IsNewUser: false);

        return Result<AuthenticationResponse>.Success(response);
    }
}
