using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json.Serialization;
using Sportner.API.Middleware;
using Sportner.API.Services;
using Sportner.API.Extensions.Swagger;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Infrastructure.Transformers;

namespace Sportner.API.Extensions.Collection;

public static class CustomServiceCollectionExtensions
{
    public static IServiceCollection AddCustomCollection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services
            .AddControllers(options =>
            {
                options.Conventions.Add(
                    new RouteTokenTransformerConvention(
                        new KebabCaseParameterTransformer()));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver =
                    new CamelCasePropertyNamesContractResolver();
            });

        services.AddHttpContextAccessor();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddCustomSwagger();

        return services;
    }
}
