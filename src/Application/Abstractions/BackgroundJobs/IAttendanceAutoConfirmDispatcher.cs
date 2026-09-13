namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IAttendanceAutoConfirmDispatcher
{
    /// <summary>
    /// Safety net for organizers who never take attendance: once a completed event is older
    /// than the configured grace period, every still-Approved participant is auto-confirmed as
    /// Attended so the review flow doesn't stay blocked on an organizer who never returns.
    /// Returns how many participants were auto-confirmed.
    /// </summary>
    Task<int> DispatchAsync(CancellationToken cancellationToken = default);
}
