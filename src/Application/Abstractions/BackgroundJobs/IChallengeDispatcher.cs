namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IChallengeDispatcher
{
    /// <summary>
    /// Opens this month's challenges, marks completions and sends challenge notifications.
    /// Idempotent; safe to run hourly.
    /// </summary>
    Task DispatchAsync(CancellationToken cancellationToken = default);
}
