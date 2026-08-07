namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// Short-lived OTP challenge storage. Codes are stored hashed; never log the raw code.
/// </summary>
public interface IOtpChallengeStore
{
    Task SetAsync(
        string phoneNumber,
        string codeHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<string?> GetHashAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<int> RemoveExpiredAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);
}
