using Microsoft.Extensions.Logging;
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

/// <summary>
/// Bu bileşim API ve üç worker tarafından paylaşılıyor, ortamları ise aynı
/// değil: worker'lar yalnızca cron işleri çalıştırıyor, depolama/e-posta/adres
/// anahtarlarını taşımıyorlar.
///
/// <para><b>Kural:</b> <see cref="AddInfrastructure"/> içinde açılışta zorunlu
/// kılınan her şey, her host'ta gerçekten bulunan bir şey olmalı. Bir host'a
/// özel zorunluluk, o host'un kendi çağırdığı ayrı bir metoda taşınır —
/// <see cref="ValidateSocialAuthOptionsOnStart"/> bunun örneği.</para>
///
/// <para>Bu kural Apple/Google seçenekleri burada <c>ValidateOnStart</c> ile
/// bağlandığı ve üç worker birden açılışta çöktüğü için yazıldı. Aynı tuzak
/// depolama, e-posta ve Places seçenekleri için de geçerli; hepsi bilerek
/// doğrulamasız bağlanıyor. <c>SharedCompositionTests</c> bu sözleşmeyi
/// koruyor: buraya açılış doğrulaması eklenirse test kırmızıya döner.</para>
/// </summary>
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

    /// <summary>
    /// Sosyal giriş yapılandırmasını açılışta zorunlu kılar. Yalnızca API
    /// çağırmalı: Apple/Google token doğrulaması orada yapılıyor.
    ///
    /// Eksik yapılandırma sessizce geçtiğinde doğrulayıcılar boş string'i
    /// beklenen audience sanıp her token'ı reddediyor ve kullanıcı "kimlik
    /// doğrulanamadı" görüyordu — uygulama ise sorunsuz ayağa kalkmış
    /// oluyordu. Bu yüzden API'de açılışta patlaması tercih ediliyor.
    /// </summary>
    public static IServiceCollection ValidateSocialAuthOptionsOnStart(
        this IServiceCollection services)
    {
        services.AddOptions<GoogleAuthOptions>().ValidateOnStart();
        services.AddOptions<AppleAuthOptions>().ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Yalnızca bağlanıyor, burada doğrulanmıyor: bu metot API ile birlikte
        // üç worker tarafından da çağrılıyor ve worker'lar sosyal giriş
        // yapmadığı için bu anahtarlar onların ortamında tanımlı değil.
        // Doğrulamayı API kendi başlangıcında açıyor:
        // ValidateSocialAuthOptionsOnStart()
        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddOptions<AppleAuthOptions>()
            .Bind(configuration.GetSection(AppleAuthOptions.SectionName))
            .ValidateDataAnnotations();

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
        // Doğrulamasız bağlanıyor. Depolamayı yalnızca API kullanıyor, ama bu
        // metot worker'larda da çalışıyor: buraya ValidateOnStart eklemek
        // onları açılışta düşürür. Zorunlu kılmak gerekirse API'nin kendi
        // çağırdığı bir metoda koyun — ValidateSocialAuthOptionsOnStart gibi.
        services.Configure<SupabaseStorageOptions>(
            configuration.GetSection(SupabaseStorageOptions.SectionName));

        // Somut tip olarak kaydedilip sarmalanıyor: yüklenen görseller depoya
        // yazılmadan önce küçültülsün. IFileStorage bütün yükleme yollarının
        // tek geçiş noktası, o yüzden altı çağrı yerinin hiçbirine dokunmak
        // gerekmiyor — ileride eklenecekler de kapsanıyor.
        services.AddHttpClient<SupabaseFileStorage>();

        services.AddTransient<IFileStorage>(provider => new ResizingFileStorage(
            provider.GetRequiredService<SupabaseFileStorage>(),
            provider.GetRequiredService<ILogger<ResizingFileStorage>>()));

        return services;
    }

    private static IServiceCollection AddLocationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Doğrulamasız bağlanıyor: anahtar boşsa Nominatim'e düşüyoruz, ayrıca
        // bu metot worker'larda da çalışıyor. Bkz. sınıf başındaki kural.
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
        // Doğrulamasız bağlanıyor: anahtar boşsa loglayan gönderene düşüyoruz,
        // ayrıca bu metot worker'larda da çalışıyor. Bkz. sınıf başındaki kural.
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
