using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Common.Results;

namespace Sportner.Infrastructure.Authentication;

public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private static readonly Error InvalidTokenError = Error.Unauthorized(
        "ExternalAuth.InvalidGoogleToken",
        "Google identity token could not be verified.");

    private readonly GoogleAuthOptions _options;

    public GoogleTokenVerifier(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Result<ExternalIdentity>> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_options.WebClientId]
                });

            return Result<ExternalIdentity>.Success(
                new ExternalIdentity(
                    payload.Subject,
                    payload.Email,
                    payload.GivenName,
                    payload.FamilyName,
                    payload.Picture));
        }
        catch (InvalidJwtException)
        {
            return Result<ExternalIdentity>.Failure(InvalidTokenError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException
            or IOException
            or InvalidOperationException)
        {
            return Result<ExternalIdentity>.Failure(InvalidTokenError);
        }
    }
}
