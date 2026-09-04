using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Authentication;

public interface IAppleTokenVerifier
{
    Task<Result<ExternalIdentity>> VerifyAsync(string identityToken, CancellationToken cancellationToken);
}
