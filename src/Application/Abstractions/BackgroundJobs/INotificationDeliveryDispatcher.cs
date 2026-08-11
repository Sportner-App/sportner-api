namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface INotificationDeliveryDispatcher
{
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}
