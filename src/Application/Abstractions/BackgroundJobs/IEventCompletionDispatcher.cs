namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IEventCompletionDispatcher
{
    /// <summary>
    /// Completes published/full events whose scheduled end has passed.
    /// Returns how many events transitioned to Completed.
    /// </summary>
    Task<int> DispatchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a single event if it is due. Idempotent. Returns whether status changed.
    /// </summary>
    Task<bool> CompleteIfDueAsync(Guid eventId, CancellationToken cancellationToken = default);
}
