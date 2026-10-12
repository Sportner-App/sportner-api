using Microsoft.AspNetCore.HttpOverrides;

namespace Sportner.API.Extensions.Forwarding;

public static class ForwardedHeadersExtension
{
    /// <summary>
    /// Railway (and any reverse proxy) terminates TLS and forwards the request over the
    /// internal network, so <c>HttpContext.Connection.RemoteIpAddress</c> is the proxy's
    /// address — identical for every caller. Without this, IP-partitioned rate limiters
    /// put all traffic in a single bucket: brute-force protection disappears and one
    /// caller hitting the limit locks everyone out. <c>UseHttpsRedirection</c> likewise
    /// needs X-Forwarded-Proto to see that the original request was already HTTPS.
    /// </summary>
    public static IServiceCollection AddCustomForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // The container is only reachable through the platform's edge proxy, which
            // appends its own entry to X-Forwarded-For. Reading a single hop from the
            // right therefore yields the address the proxy observed, not a value the
            // client supplied — a spoofed header stays to the left and is ignored.
            options.ForwardLimit = 1;

            // The proxy's address is assigned dynamically, so it cannot be allow-listed.
            // Clearing these is what makes the headers trusted at all; safe only because
            // of the single-entry-point assumption above.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
