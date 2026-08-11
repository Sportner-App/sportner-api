using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.Login;

public sealed record LoginCommand(
    string Username,
    string Password,
    string? IpAddress,
    string? UserAgent) : ICommand<AuthenticationResponse>;
