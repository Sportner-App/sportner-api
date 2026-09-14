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

            // Several controllers declare nested request DTOs with the same short name
            // (e.g. BlockUserBody in both BlocksController and FriendshipsController) -
            // Swashbuckle's default schemaId is just the type name, so those collide and
            // crash schema generation. Prefix with the declaring type to keep IDs unique.
            options.CustomSchemaIds(type => type.DeclaringType is not null
                ? $"{type.DeclaringType.Name}{type.Name}"
                : type.Name);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter the JWT access token as: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });
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
