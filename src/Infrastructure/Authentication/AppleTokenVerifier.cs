using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Common.Results;

namespace Sportner.Infrastructure.Authentication;

public sealed class AppleTokenVerifier : IAppleTokenVerifier
{
    private static readonly Error InvalidTokenError = Error.Unauthorized(
        "ExternalAuth.InvalidAppleToken",
        "Apple identity token could not be verified.");

    private static readonly JsonWebTokenHandler Handler = new();

    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly AppleAuthOptions _options;

    public AppleTokenVerifier(
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        IOptions<AppleAuthOptions> options)
    {
        _configurationManager = configurationManager;
        _options = options.Value;
    }

    public async Task<Result<ExternalIdentity>> VerifyAsync(
        string identityToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.BundleId,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true
        };

        var result = await Handler.ValidateTokenAsync(identityToken, validationParameters);

        if (!result.IsValid
            || !result.Claims.TryGetValue("sub", out var subjectClaim)
            || subjectClaim?.ToString() is not { Length: > 0 } subject)
        {
            return Result<ExternalIdentity>.Failure(InvalidTokenError);
        }

        var email = result.Claims.TryGetValue("email", out var emailClaim)
            ? emailClaim?.ToString()
            : null;

            return Result<ExternalIdentity>.Success(new ExternalIdentity(subject, email));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is SecurityTokenException
            or IOException
            or HttpRequestException
            or InvalidOperationException)
        {
            return Result<ExternalIdentity>.Failure(InvalidTokenError);
        }
    }
}
