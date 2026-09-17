using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Email;

namespace Sportner.Infrastructure.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private const string SendEndpoint = "https://api.resend.com/emails";

    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<EmailSendResult> SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        var request = new ResendEmailRequest(
            _options.FromAddress,
            [toEmail],
            "Sportner doğrulama kodun",
            $"""
             <p>Sportner hesabını doğrulamak için kodun:</p>
             <p style="font-size:28px;font-weight:700;letter-spacing:4px;">{code}</p>
             <p>Bu kod 15 dakika içinde geçerliliğini yitirecek.</p>
             """);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                SendEndpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return EmailSendResult.Ok();
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = $"Resend returned HTTP {(int)response.StatusCode}.";
            _logger.LogWarning("{Error} Response: {Response}", error, Truncate(errorBody, 500));

            return EmailSendResult.Failed(error);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EmailSendResult.Failed("Resend request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Resend request failed.");
            return EmailSendResult.Failed("Resend could not be reached.");
        }
    }

    public async Task<EmailSendResult> SendPasswordResetCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        var request = new ResendEmailRequest(
            _options.FromAddress,
            [toEmail],
            "Sportner şifre sıfırlama kodun",
            $"""
             <p>Sportner hesabının şifresini sıfırlamak için kodun:</p>
             <p style="font-size:28px;font-weight:700;letter-spacing:4px;">{code}</p>
             <p>Bu kod 15 dakika içinde geçerliliğini yitirecek.</p>
             <p>Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin.</p>
             """);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                SendEndpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return EmailSendResult.Ok();
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = $"Resend returned HTTP {(int)response.StatusCode}.";
            _logger.LogWarning("{Error} Response: {Response}", error, Truncate(errorBody, 500));

            return EmailSendResult.Failed(error);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EmailSendResult.Failed("Resend request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Resend request failed.");
            return EmailSendResult.Failed("Resend could not be reached.");
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}
