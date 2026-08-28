using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Notifications;

namespace Sportner.Infrastructure.Notifications;

public sealed class ExpoPushSender : IPushSender
{
    private const string SendEndpoint = "https://exp.host/--/api/v2/push/send";
    private const string DeviceNotRegistered = "DeviceNotRegistered";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ExpoPushSender> _logger;

    public ExpoPushSender(HttpClient httpClient, ILogger<ExpoPushSender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PushSendResult> SendAsync(
        PushMessage message,
        CancellationToken cancellationToken = default)
    {
        var request = new ExpoPushRequest(
            message.PushToken,
            message.Title,
            message.Body,
            "default",
            "high",
            "default",
            new ExpoPushData(
                (short)message.NotificationType,
                (short)message.EntityType,
                message.EntityId?.ToString("D")));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                SendEndpoint,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = $"Expo push service returned HTTP {(int)response.StatusCode}.";

                _logger.LogWarning(
                    "{Error} Response: {Response}",
                    error,
                    Truncate(errorBody, 500));

                return PushSendResult.Failed(error);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return PushSendResult.Failed("Expo push response did not contain a ticket.");
            }

            var ticket = data.ValueKind == JsonValueKind.Array
                ? data.EnumerateArray().FirstOrDefault()
                : data;

            if (ticket.ValueKind != JsonValueKind.Object)
            {
                return PushSendResult.Failed("Expo push response ticket was invalid.");
            }

            var status = GetString(ticket, "status");
            if (string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return PushSendResult.Ok();
            }

            var errorCode = ticket.TryGetProperty("details", out var details)
                ? GetString(details, "error")
                : null;
            var errorMessage = GetString(ticket, "message") ?? "Expo rejected the push message.";

            return string.Equals(errorCode, DeviceNotRegistered, StringComparison.Ordinal)
                ? PushSendResult.Invalid(errorMessage)
                : PushSendResult.Failed(errorMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PushSendResult.Failed("Expo push request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Expo push service request failed.");
            return PushSendResult.Failed("Expo push service could not be reached.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Expo push service returned invalid JSON.");
            return PushSendResult.Failed("Expo push service returned an invalid response.");
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ExpoPushRequest(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("sound")] string Sound,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("channelId")] string ChannelId,
        [property: JsonPropertyName("data")] ExpoPushData Data);

    private sealed record ExpoPushData(
        [property: JsonPropertyName("notificationType")] short NotificationType,
        [property: JsonPropertyName("entityType")] short EntityType,
        [property: JsonPropertyName("entityId")] string? EntityId);
}

