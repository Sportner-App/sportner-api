namespace Sportner.API.Extensions.Cors;

public static class CorsExtensions
{
    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
                else
                {
                    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
            });
        });

        return services;
    }
}
