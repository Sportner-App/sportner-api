namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IEngagementReminderDispatcher
{
    /// <summary>
    /// Sends at most one engagement reminder (streak, weekly goal, new events, come back)
    /// per user per day. Returns sent count.
    /// </summary>
    Task<int> DispatchAsync(CancellationToken cancellationToken = default);
}
