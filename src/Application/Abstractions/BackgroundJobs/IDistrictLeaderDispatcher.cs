namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IDistrictLeaderDispatcher
{
    /// <summary>
    /// Tracks this month's district leaders (notifying new #1s) and crowns last month's.
    /// Idempotent; safe to run hourly.
    /// </summary>
    Task DispatchAsync(CancellationToken cancellationToken = default);
}
