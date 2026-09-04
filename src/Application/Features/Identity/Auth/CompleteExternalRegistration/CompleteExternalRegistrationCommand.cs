using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.CompleteExternalRegistration;

public sealed record CompleteExternalRegistrationCommand(
    string RegistrationToken,
    string Username,
    string FirstName,
    string? LastName,
    DateOnly BirthDate,
    short Gender,
    string? IpAddress,
    string? UserAgent) : ICommand<AuthenticationResponse>;
