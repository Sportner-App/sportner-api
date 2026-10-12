namespace Sportner.API.Middleware;

/// <summary>
/// Yanitlara guvenlik basliklarini ekler.
///
/// Servis bir JSON API: tarayicida render edilecek sayfa dondurmuyor. Yine de
/// hata sayfalari, yanlis yapilandirilmis bir Content-Type ya da ileride
/// eklenecek herhangi bir HTML yanit tarayiciya ulasabiliyor; bu basliklar o
/// yollarin hepsini tek yerden kapatiyor.
///
/// Swagger UI kendi inline script ve stillerini calistirdigi icin CSP'den muaf
/// tutuluyor (zaten yalnizca Development'ta aciliyor, bkz. Program.cs).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private const string SwaggerPathPrefix = "/swagger";

    /// <summary>
    /// API hicbir kaynak yuklemiyor, bu yuzden en dar politika: hicbir sey.
    /// <c>frame-ancestors</c> clickjacking'i X-Frame-Options'in modern
    /// karsiligiyla da engeller.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Tarayicinin Content-Type'i tahmin etmesini engeller: JSON olarak
            // dondurulen bir govdenin HTML gibi yorumlanmasini onler.
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";

            // Istek yollari kimlik (ve SignalR'da token) tasiyabiliyor; disari
            // referrer sizmasin.
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";

            if (!context.Request.Path.StartsWithSegments(SwaggerPathPrefix))
            {
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
            }

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
