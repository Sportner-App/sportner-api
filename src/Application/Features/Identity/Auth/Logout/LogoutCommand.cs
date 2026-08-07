using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
