using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Authentication;

public interface IExternalRegistrationTokenService
{
    ExternalRegistrationToken Create(ExternalRegistrationTicket ticket);

    Task<Result<ExternalRegistrationTicket>> ValidateAsync(
        string token,
        CancellationToken cancellationToken);
}
