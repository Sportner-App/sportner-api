using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Sportner.API.Extensions.RateLimiting;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string ReportPolicy = "reports";
    public const string FeedbackPolicy = "feedback";
    public const string EventQnAPolicy = "event-qna";
    public const string PasswordResetPolicy = "password-reset";

    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Her isteğe uygulanan genel tavan; uç noktaya özel politikalar bunun üstüne
            // ayrıca uygulanır. Amaç toplu veri çekmeyi (ID'leri sırayla gezip etkinlik,
            // katılımcı, profil toplamak) zorlaştırmak. Normal kullanım bu sınırlara yaklaşmaz.
            // Girişli istekler kullanıcı başına, girişsizler IP başına sayılır. Bu yüzden
            // UseRateLimiter, Program.cs'te UseAuthentication'dan sonra gelmeli.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                if (!configuration.GetValue("RateLimiting:GlobalEnabled", true)
                    || httpContext.Request.Path.StartsWithSegments("/health"))
                {
                    return RateLimitPartition.GetNoLimiter("unlimited");
                }

                if (httpContext.User.FindFirst("sub")?.Value is { } userId)
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"user:{userId}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 300,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
                }

                // Mobil operatörlerde (CGNAT) çok kullanıcı tek IP'yi paylaşır; sınır bu
                // yüzden geniş. Script'le toplu çekmeyi yine de dakikada birkaç yüz isteğe indirir.
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"ip:{ClientNetworkKey(httpContext)}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    }));

            options.AddPolicy(ReportPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(FeedbackPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(EventQnAPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    }));

            // Tighter than AuthPolicy: this endpoint is anonymous and email-keyed rather than
            // account-keyed, so it's the natural target for enumerating valid emails or spamming
            // a victim's inbox — a stricter IP ceiling on top of the per-account resend cooldown
            // in ForgotPasswordCommandHandler gives defense in depth.
            options.AddPolicy(PasswordResetPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    /// <summary>
    /// IPv4 adresini olduğu gibi, IPv6 adresini /64 ağına indirerek döndürür: bir IPv6
    /// istemcisi /64 içinde istediği kadar adres üretebildiği için tam adresle sayılırsa
    /// her istekte adres değiştirip sınırı aşabilir.
    /// </summary>
    private static string ClientNetworkKey(HttpContext httpContext)
    {
        var address = httpContext.Connection.RemoteIpAddress;
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new System.Net.IPAddress(bytes)}/64";
    }
}
