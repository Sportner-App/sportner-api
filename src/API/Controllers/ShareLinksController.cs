using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sportner.API.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ShareLinksController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ShareLinksController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("share/events/{eventId:guid}")]
    public ContentResult Event(Guid eventId)
    {
        // Triple slash keeps `events` in the path instead of treating it as a host,
        // so Expo Router resolves the URL to app/events/[id].tsx.
        var deepLink = $"sportner:///events/{eventId:D}";
        var iosStoreUrl = _configuration["AppLinks:IosStoreUrl"]
            ?? "https://apps.apple.com/tr/search?term=Sportner";
        var androidStoreUrl = _configuration["AppLinks:AndroidStoreUrl"]
            ?? "https://play.google.com/store/search?q=Sportner&c=apps";

        var encoder = HtmlEncoder.Default;
        var html = $$"""
            <!doctype html>
            <html lang="tr">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1" />
              <meta name="theme-color" content="#06111a" />
              <meta property="og:title" content="Sportner etkinliğine katıl" />
              <meta property="og:description" content="Etkinliği Sportner uygulamasında görüntüle." />
              <title>Sportner etkinliği</title>
              <style>
                *{box-sizing:border-box}body{margin:0;background:#06111a;color:#f4f6f2;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;min-height:100vh;display:grid;place-items:center;padding:24px}.card{width:min(100%,420px);padding:32px 24px;border:1px solid #243746;border-radius:28px;background:#0d1d29;text-align:center}.dot{width:14px;height:14px;border-radius:50%;background:#ccff00;margin:0 auto 18px}h1{font-size:28px;margin:0 0 10px}p{color:#a8b2b8;line-height:1.5;margin:0 0 24px}.button{display:block;width:100%;padding:16px;border-radius:18px;background:#ccff00;color:#06111a;font-weight:800;text-decoration:none}.muted{display:block;margin-top:16px;color:#a8b2b8;font-size:13px}
              </style>
            </head>
            <body>
              <main class="card">
                <div class="dot"></div>
                <h1>Sportner'da aç</h1>
                <p>Paylaşılan etkinliği görüntülemek için Sportner uygulamasına geç.</p>
                <a class="button" id="open-app" href="{{encoder.Encode(deepLink)}}">Etkinliği aç</a>
                <a class="muted" id="store-link" href="#">Uygulama yoksa mağazadan indir</a>
              </main>
              <script>
                (() => {
                  const deepLink = {{System.Text.Json.JsonSerializer.Serialize(deepLink)}};
                  const iosStore = {{System.Text.Json.JsonSerializer.Serialize(iosStoreUrl)}};
                  const androidStore = {{System.Text.Json.JsonSerializer.Serialize(androidStoreUrl)}};
                  const isAndroid = /Android/i.test(navigator.userAgent);
                  const storeUrl = isAndroid ? androidStore : iosStore;
                  const storeLink = document.getElementById('store-link');
                  storeLink.href = storeUrl;

                  let fallbackTimer;
                  const openApp = () => {
                    clearTimeout(fallbackTimer);
                    fallbackTimer = setTimeout(() => location.replace(storeUrl), 1400);
                    location.href = deepLink;
                  };

                  document.addEventListener('visibilitychange', () => {
                    if (document.hidden) clearTimeout(fallbackTimer);
                  });
                  window.addEventListener('pagehide', () => clearTimeout(fallbackTimer));
                  document.getElementById('open-app').addEventListener('click', (event) => {
                    event.preventDefault();
                    openApp();
                  });
                  setTimeout(openApp, 250);
                })();
              </script>
            </body>
            </html>
            """;

        return Content(html, "text/html; charset=utf-8");
    }
}
