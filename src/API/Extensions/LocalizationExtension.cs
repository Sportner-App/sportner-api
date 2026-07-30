using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace Sportner.API.Extensions;

public static class LocalizationExtension
{
    private static readonly List<CultureInfo> SupportedCultures =
    [
        new CultureInfo("en-US"),
        new CultureInfo("tr-TR")
    ];

    private static readonly List<CultureInfo> SupportedDataCultures =
    [
        new CultureInfo("en-US")
    ];

    public static IServiceCollection AddCustomLocalization(this IServiceCollection services)
    {
        services.AddLocalization();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");
            options.SupportedCultures = SupportedDataCultures;
            options.SupportedUICultures = SupportedCultures;
            options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
        });

        return services;
    }

    public static IApplicationBuilder UseCustomLocalization(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<RequestLocalizationOptions>>()
            ?.Value;

        if (options != null)
        {
            app.UseRequestLocalization(options);
        }

        return app;
    }
}
