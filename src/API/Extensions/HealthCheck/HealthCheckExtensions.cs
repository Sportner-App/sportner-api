using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Sportner.API.Extensions.HealthCheck;

public static class HealthCheckExtensions
{
    public static void UseAppHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready");
    }
}
