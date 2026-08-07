using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthenticationResponse>;
