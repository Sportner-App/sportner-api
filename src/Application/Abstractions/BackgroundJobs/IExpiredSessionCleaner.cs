namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IExpiredSessionCleaner
{
    /// <summary>
    /// Deletes revoked/expired sessions older than the configured retention. Returns deleted count.
    /// </summary>
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}
