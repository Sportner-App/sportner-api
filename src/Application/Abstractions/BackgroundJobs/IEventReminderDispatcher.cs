namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IEventReminderDispatcher
{
    /// <summary>
    /// Sends due event reminders (idempotent per event/user/window). Returns sent count.
    /// </summary>
    Task<int> DispatchAsync(CancellationToken cancellationToken = default);
}
