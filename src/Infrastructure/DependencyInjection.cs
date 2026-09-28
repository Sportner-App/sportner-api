using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Email;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Infrastructure.Authentication;
using Sportner.Infrastructure.Email;
using Sportner.Infrastructure.Notifications;
using Sportner.Infrastructure.Persistence;
using Sportner.Infrastructure.Persistence.Interceptors;
using Sportner.Infrastructure.Persistence.Seed;
using Sportner.Infrastructure.Storage;

using Sportner.Application.Abstractions.Location;
using Sportner.Infrastructure.Location;
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
        services.AddLocationServices(configuration);
        services.AddScoped<INotificationPublisher, InAppNotificationPublisher>();
        services.AddHttpClient<IPushSender, ExpoPushSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddEmailServices(configuration);

        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Sosyal giriş ayarları eksik olduğunda uygulama sorunsuz ayağa kalkıp
        // yalnızca Apple/Google girişini bozuyordu: doğrulayıcılar boş string'i
        // audience olarak kullanıp her token'ı reddediyor, kullanıcıya da genel
        // bir "doğrulanamadı" mesajı dönüyordu. Artık eksik yapılandırma
        // açılışta hata veriyor, sessizce üretime çıkmıyor.
        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AppleAuthOptions>()
            .Bind(configuration.GetSection(AppleAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

    private static IServiceCollection AddLocationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GooglePlacesOptions>(
            configuration.GetSection(GooglePlacesOptions.SectionName));

        services.AddHttpClient<GooglePlaceProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddHttpClient<NominatimPlaceProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            // Nominatim's usage policy requires an identifying User-Agent and
            // rejects requests without one.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "SportnerApi/1.0 (contact@sportner.app)");
        });

        // No Google Places key configured (local dev, or before billing is set
        // up) → fall back to Nominatim so address search still works, instead
        // of failing every lookup. Same shape as the email sender fallback.
        services.AddScoped<IPlaceProvider>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<GooglePlacesOptions>>().Value;

            return string.IsNullOrWhiteSpace(options.ApiKey)
                ? provider.GetRequiredService<NominatimPlaceProvider>()
                : provider.GetRequiredService<GooglePlaceProvider>();
        });

        return services;
    }

    private static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));

        services.AddHttpClient<ResendEmailSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        services.AddScoped<LoggingEmailSender>();

        // No Resend API key configured yet (e.g. local/dev, or before the account is set up) →
        // fall back to logging the code instead of failing registration/verification outright.
        services.AddScoped<IEmailSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ResendOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.ApiKey)
                ? provider.GetRequiredService<LoggingEmailSender>()
                : provider.GetRequiredService<ResendEmailSender>();
        });

        return services;
    }
}
