using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.SignInWithGoogle;

public sealed record SignInWithGoogleCommand(
    string IdToken,
    string? IpAddress,
    string? UserAgent) : ICommand<ExternalSignInResponse>;
