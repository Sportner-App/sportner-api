using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Authentication;

public interface IGoogleTokenVerifier
{
    Task<Result<ExternalIdentity>> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
