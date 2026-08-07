namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IOtpCleaner
{
    /// <summary>
    /// Removes expired OTP challenges from the challenge store. Returns removed count.
    /// </summary>
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}
