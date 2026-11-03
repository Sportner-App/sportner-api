namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IMonthlyRecapDispatcher
{
    /// <summary>
    /// Tells users with enough games last month that their monthly recap is ready.
    /// Once per user per month; safe to run hourly.
    /// </summary>
    Task DispatchAsync(CancellationToken cancellationToken = default);
}
