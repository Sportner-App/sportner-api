using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.Register;

public sealed record RegisterCommand(
    string Username,
    string Password,
    string FirstName,
    string? LastName,
    string? IpAddress,
    string? UserAgent) : ICommand<AuthenticationResponse>;
