using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Infrastructure.Authentication;

public sealed class ExternalRegistrationTokenService : IExternalRegistrationTokenService
{
    private const string TokenType = "external-registration";
    private static readonly Error InvalidToken = Error.Unauthorized(
        "Auth.ExternalRegistrationTokenInvalid",
        "External registration token is invalid or expired.");
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public ExternalRegistrationTokenService(
        IOptions<JwtSettings> settings,
        TimeProvider timeProvider)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public ExternalRegistrationToken Create(ExternalRegistrationTicket ticket)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(15);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, ticket.ProviderUserId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type", TokenType),
            new("provider", ((short)ticket.Provider).ToString())
        };
        AddOptional(claims, JwtRegisteredClaimNames.Email, ticket.Email);
        AddOptional(claims, "first_name", ticket.FirstName);
        AddOptional(claims, "last_name", ticket.LastName);
        AddOptional(claims, "profile_image_url", ticket.ProfileImageUrl);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = $"{_settings.Audience}:external-registration",
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
                SecurityAlgorithms.HmacSha256)
        };

        return new ExternalRegistrationToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    public async Task<Result<ExternalRegistrationTicket>> ValidateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = _settings.Issuer,
            ValidAudience = $"{_settings.Audience}:external-registration",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            LifetimeValidator = (notBefore, expires, _, parameters) =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                return (!notBefore.HasValue || notBefore.Value <= now + parameters.ClockSkew)
                    && expires.HasValue
                    && expires.Value >= now - parameters.ClockSkew;
            }
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsValid
            || Claim(result, "token_type") != TokenType
            || !Enum.TryParse<ExternalLoginProvider>(Claim(result, "provider"), out var provider)
            || Claim(result, JwtRegisteredClaimNames.Sub) is not { Length: > 0 } providerUserId)
        {
            return Result<ExternalRegistrationTicket>.Failure(InvalidToken);
        }

        return Result<ExternalRegistrationTicket>.Success(new ExternalRegistrationTicket(
            provider,
            providerUserId,
            Claim(result, JwtRegisteredClaimNames.Email),
            Claim(result, "first_name"),
            Claim(result, "last_name"),
            Claim(result, "profile_image_url")));
    }

    private static string? Claim(TokenValidationResult result, string name) =>
        result.Claims.TryGetValue(name, out var value) ? value?.ToString() : null;

    private static void AddOptional(ICollection<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }
}
