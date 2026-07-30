namespace SportnerApi.Services;

public interface INotificationService
{
    Task SendPushNotificationAsync(
        string? pushToken,
        string title,
        string body,
        object? data = null,
        CancellationToken cancellationToken = default);
}
