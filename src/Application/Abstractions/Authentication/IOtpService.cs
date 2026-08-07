namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// Generates, stores (hashed, short TTL) and verifies one-time passwords for phone login.
/// Implementations must never log the OTP code.
/// </summary>
public interface IOtpService
{
    Task RequestAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);
}
