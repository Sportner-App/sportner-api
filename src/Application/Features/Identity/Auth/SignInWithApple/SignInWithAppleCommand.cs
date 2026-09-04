using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.SignInWithApple;

/// <summary>
/// <see cref="FirstName"/>/<see cref="LastName"/> come from Apple's native one-time callback
/// (the identity token itself never carries a name) — accepted for parity/future use even
/// though account creation here doesn't need them yet.
/// </summary>
public sealed record SignInWithAppleCommand(
    string IdentityToken,
    string? FirstName,
    string? LastName,
    string? IpAddress,
    string? UserAgent) : ICommand<ExternalSignInResponse>;
