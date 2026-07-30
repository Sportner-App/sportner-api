using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SportnerApi.Services;

public class NotificationService(
    HttpClient httpClient,
    ILogger<NotificationService> logger) : INotificationService
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";

    public async Task SendPushNotificationAsync(
        string? pushToken,
        string title,
        string body,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pushToken) ||
            !pushToken.StartsWith("ExponentPushToken", StringComparison.Ordinal))
        {
            return;
        }

        var payload = new ExpoPushMessage
        {
            To = pushToken,
            Title = title,
            Body = body,
            Data = data,
            Sound = "default"
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                ExpoPushUrl,
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Expo push failed ({StatusCode}): {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Expo push notification to {PushToken}", pushToken);
        }
    }

    private sealed class ExpoPushMessage
    {
        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("sound")]
        public string Sound { get; set; } = "default";
    }
}
