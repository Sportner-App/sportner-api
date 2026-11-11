using System.Net;
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

    public Task<EmailSendResult> SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            EmailTemplate.Code(
                subject: $"Sportner doğrulama kodun: {code}",
                intro: "Sportner'a hoş geldin! Hesabını doğrulamak için aşağıdaki kodu uygulamaya gir.",
                code: code,
                validity: "Kod 15 dakika geçerli.",
                reason: "Bu e-postayı Sportner'da bu adresle hesap oluşturulduğu için aldın. Kayıt olan sen değilsen e-postayı yok sayabilirsin."),
            replyTo: null,
            cancellationToken);

    public Task<EmailSendResult> SendPasswordResetCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            EmailTemplate.Code(
                subject: $"Sportner şifre sıfırlama kodun: {code}",
                intro: "Şifreni sıfırlamak için aşağıdaki kodu uygulamaya gir.",
                code: code,
                validity: "Kod 15 dakika geçerli.",
                reason: "Bu e-postayı Sportner hesabın için şifre sıfırlama istendiği için aldın. İsteği sen yapmadıysan e-postayı yok sayabilirsin; şifren değişmez."),
            replyTo: null,
            cancellationToken);

    public Task<EmailSendResult> SendAdminLoginCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            EmailTemplate.Code(
                subject: $"Sportner yönetim paneli giriş kodu: {code}",
                intro: "Yönetim paneline giriş kodun:",
                code: code,
                validity: "Kod 10 dakika geçerli.",
                reason: "Giriş yapmaya çalışan sen değilsen bu e-postayı yok say."),
            replyTo: null,
            cancellationToken);

    public Task<EmailSendResult> SendFeedbackReplyAsync(
        string toEmail,
        string originalFeedback,
        string reply,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            EmailTemplate.FeedbackReply(reply, originalFeedback),
            replyTo: _options.ReplyToAddress,
            cancellationToken);

    private async Task<EmailSendResult> SendAsync(
        string toEmail,
        EmailContent content,
        string? replyTo,
        CancellationToken cancellationToken)
    {
        var request = new ResendEmailRequest(
            _options.FromAddress,
            [toEmail],
            content.Subject,
            content.Html,
            content.Text,
            string.IsNullOrWhiteSpace(replyTo) ? null : replyTo);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(SendEndpoint, request, cancellationToken);

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
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("reply_to"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReplyTo);
}

internal sealed record EmailContent(string Subject, string Html, string Text);

/// <summary>
/// Tüm işlem e-postalarının ortak kalıbı: tam HTML belgesi, logo, alt bilgi ve aynı
/// içeriğin düz metin sürümü. Yalnızca HTML'den oluşan, gövdesi tek satır kod olan
/// e-postalar (özellikle Outlook/Hotmail'de) spam puanını yükseltiyor.
/// </summary>
internal static class EmailTemplate
{
    private const string SiteUrl = "https://sportner.app";
    private const string LogoUrl = SiteUrl + "/logo.png";
    private const string Font = "-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif";

    // Uygulama ve sitenin imza renkleri (sportner-site globals.css ile aynı).
    private const string Bg = "#06111a";
    private const string Surface = "#0d1b27";
    private const string Surface2 = "#152635";
    private const string Line = "#203443";
    private const string Lime = "#ccff00";
    private const string Ink = "#f4f6f2";
    private const string Muted = "#a8b2b8";
    private const string Soft = "#6f7d86";

    /// <summary>Spor renklerinden oluşan ince şerit: futbol, basketbol, voleybol, koşu, tenis.</summary>
    private static readonly string[] SportColors = ["#9ed900", "#ff6b1a", "#9a72ff", "#5eead4", "#fbbf24"];

    public static EmailContent Code(string subject, string intro, string code, string validity, string reason)
    {
        var body = $"""
            <p style="margin:0 0 24px;color:{Ink};">{Encode(intro)}</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
            <td align="center" bgcolor="{Lime}" style="background-color:{Lime};border-radius:16px;padding:22px 0;font-family:'SF Mono',Menlo,Consolas,monospace;font-size:36px;font-weight:800;letter-spacing:10px;color:{Bg};">{Encode(code)}</td>
            </tr></table>
            <p style="margin:18px 0 0;color:{Muted};font-size:13px;">⏱ {Encode(validity)}</p>
            """;
        var text = $"{intro}\n\n{code}\n\n{validity}";
        return Build(subject, "Kodun hazır", body, text, reason);
    }

    public static EmailContent FeedbackReply(string reply, string originalFeedback)
    {
        const string subject = "Sportner geri bildirimine yanıt";
        var body = $"""
            <p style="margin:0 0 24px;color:{Ink};">{Multiline(reply)}</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
            <td bgcolor="{Surface2}" style="background-color:{Surface2};border-left:3px solid {Lime};border-radius:12px;padding:14px 16px;">
            <p style="margin:0 0 6px;color:{Soft};font-size:11px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;">Senin geri bildirimin</p>
            <p style="margin:0;color:{Muted};font-size:13px;">{Multiline(originalFeedback)}</p>
            </td></tr></table>
            <p style="margin:24px 0 0;color:{Ink};font-weight:700;">Sportner ekibi</p>
            """;
        var quoted = string.Join('\n', originalFeedback.Trim().Split('\n').Select(line => "> " + line));
        var text = $"{reply.Trim()}\n\nUygulamadan gönderdiğin geri bildirim:\n{quoted}\n\nSportner ekibi";
        return Build(
            subject,
            "Sana yanıt verdik",
            body,
            text,
            "Bu e-postayı Sportner uygulamasından geri bildirim gönderdiğin için aldın. Bu e-postayı yanıtlayarak bize yazabilirsin.");
    }

    private static EmailContent Build(string subject, string headline, string bodyHtml, string bodyText, string reason)
    {
        var stripe = string.Concat(SportColors.Select(color =>
            $"<td height=\"4\" bgcolor=\"{color}\" style=\"background-color:{color};font-size:0;line-height:0;\">&nbsp;</td>"));

        var html = $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="color-scheme" content="dark">
            <meta name="supported-color-schemes" content="dark">
            <title>{Encode(subject)}</title>
            </head>
            <body style="margin:0;padding:0;background:{Bg};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="{Bg}" style="background-color:{Bg};background-image:radial-gradient(ellipse at top,#1a2e0a 0%,{Bg} 60%);">
            <tr><td align="center" style="padding:40px 16px;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
            <tr><td align="center" style="padding:0 0 28px;">
            <a href="{SiteUrl}" style="text-decoration:none;font-family:{Font};font-size:16px;font-weight:800;letter-spacing:5px;color:{Ink};">
            <img src="{LogoUrl}" width="36" height="36" alt="Sportner" style="vertical-align:middle;border:0;margin-right:10px;border-radius:8px;">SPORTNER</a>
            </td></tr>
            <tr><td>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="{Surface}" style="background-color:{Surface};border:1px solid {Line};border-radius:24px;">
            <tr><td style="padding:0;border-radius:24px 24px 0 0;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>{stripe}</tr></table>
            </td></tr>
            <tr><td bgcolor="{Surface}" style="background-color:{Surface};padding:32px 32px 36px;border-radius:0 0 24px 24px;font-family:{Font};font-size:15px;line-height:1.65;color:{Ink};">
            <p style="margin:0 0 6px;color:{Lime};font-size:11px;font-weight:800;letter-spacing:2.5px;text-transform:uppercase;">Sportner</p>
            <h1 style="margin:0 0 20px;font-size:26px;line-height:1.2;font-weight:800;color:{Ink};">{Encode(headline)}</h1>
            {bodyHtml}
            </td></tr>
            </table>
            </td></tr>
            <tr><td style="padding:24px 8px 0;font-family:{Font};font-size:12px;line-height:1.6;color:{Soft};text-align:center;">
            {Encode(reason)}<br><br>
            <a href="{SiteUrl}" style="color:{Lime};text-decoration:none;font-weight:700;">sportner.app</a> · Birlikte spor yapacak birini bul.
            </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
        var text = $"{headline}\n\n{bodyText}\n\n—\n{reason}\nSportner · {SiteUrl}";
        return new EmailContent(subject, html, text);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    /// <summary>Kullanıcı ve yönetici metni HTML'e kaçışlanarak, satır sonları korunarak girer.</summary>
    private static string Multiline(string value) => Encode(value.Trim()).Replace("\n", "<br>");
}
