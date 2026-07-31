using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Sportner.API.Extensions.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = 20;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueLimit = 0;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });

        return services;
    }
}
