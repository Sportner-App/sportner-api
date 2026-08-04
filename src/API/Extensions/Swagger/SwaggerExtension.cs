using Microsoft.OpenApi;

namespace Sportner.API.Extensions.Swagger;

public static class SwaggerExtension
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Sportner API",
                Version = "v1",
                Description = "Sportner mobile backend API"
            });

            options.DescribeAllParametersInCamelCase();
        });

        return services;
    }

    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sportner API v1");
            options.RoutePrefix = "swagger";
            options.DisplayRequestDuration();
        });

        return app;
    }
}
