using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Infrastructure.Authentication;
using Sportner.Infrastructure.Notifications;
using Sportner.Infrastructure.Persistence;
using Sportner.Infrastructure.Persistence.Interceptors;
using Sportner.Infrastructure.Persistence.Seed;
using Sportner.Infrastructure.Storage;

namespace Sportner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SupabaseConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'SupabaseConnection' is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IApplicationDbContext>(
            serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        services.AddAuthenticationServices(configuration);
        services.AddStorageServices(configuration);
        services.AddScoped<INotificationPublisher, InAppNotificationPublisher>();
        services.AddHttpClient<IPushSender, ExpoPushSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));
        services.Configure<AppleAuthOptions>(configuration.GetSection(AppleAuthOptions.SectionName));

        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IExternalRegistrationTokenService, ExternalRegistrationTokenService>();

        services.AddSingleton(new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://appleid.apple.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true }));
        services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddScoped<IAppleTokenVerifier, AppleTokenVerifier>();

        return services;
    }

    private static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SupabaseStorageOptions>(
            configuration.GetSection(SupabaseStorageOptions.SectionName));

        services.AddHttpClient<IFileStorage, SupabaseFileStorage>();

        return services;
    }
}
