using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Auth.VerifyOtp;

public sealed record VerifyOtpCommand(
    string PhoneNumber,
    string Code,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<AuthenticationResponse>;
