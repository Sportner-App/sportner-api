using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

public sealed class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public JwtService(IOptions<JwtSettings> settings, TimeProvider timeProvider)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public AccessToken CreateAccessToken(Guid userId, IEnumerable<Claim>? additionalClaims = null)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddDays(_settings.ExpirationDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }

    public RefreshToken GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncoder.Encode(bytes);
        var expiresAt = _timeProvider.GetUtcNow().AddDays(_settings.RefreshTokenExpirationDays);

        return new RefreshToken(token, expiresAt);
    }
}
