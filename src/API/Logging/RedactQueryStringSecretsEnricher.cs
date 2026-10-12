using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Sportner.API.Logging;

/// <summary>
/// Log kayitlarindaki query string sirlarini maskeler.
///
/// SignalR, WebSocket upgrade isteginde access token'i query string'de tasimak
/// zorunda: tarayici ve React Native WebSocket API'leri istege header
/// ekleyemiyor (bkz. AuthenticationExtension > OnMessageReceived). Serilog'un
/// istek loglayicisi yolu ham hedeften (RawTarget) okudugu icin query string
/// de dahil oluyordu — yani gecerli bir JWT duz metin olarak loglara dusuyordu.
///
/// Enricher olarak yazildi cunku DiagnosticContext uzerinden set edilen deger
/// Serilog'un kendi RequestPath property'si tarafindan eziliyor. Enricher
/// zincirin sonunda calisir ve mesaj sablonunda kullanilsin kullanilmasin
/// property'yi duzeltir; boylece JSON sink'e gecilse de sizinti olmaz.
/// </summary>
public sealed class RedactQueryStringSecretsEnricher : ILogEventEnricher
{
    private const string Placeholder = "***";

    private static readonly string[] SensitiveKeys =
        ["access_token", "token", "refresh_token", "apikey", "api_key"];

    private static readonly Regex SecretPattern = new(
        @"(?<key>access_token|refresh_token|token|apikey|api_key)=(?<value>[^&\s]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>Serilog'un istek loglayicisinin yolu yazdigi property.</summary>
    private const string RequestPathProperty = "RequestPath";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue(RequestPathProperty, out var property))
        {
            return;
        }

        if (property is not ScalarValue { Value: string path })
        {
            return;
        }

        if (!ContainsSensitiveKey(path))
        {
            return;
        }

        logEvent.AddOrUpdateProperty(
            propertyFactory.CreateProperty(RequestPathProperty, Redact(path)));
    }

    private static bool ContainsSensitiveKey(string path) =>
        SensitiveKeys.Any(key => path.Contains(key, StringComparison.OrdinalIgnoreCase));

    private static string Redact(string path) =>
        SecretPattern.Replace(path, match => $"{match.Groups["key"].Value}={Placeholder}");
}
