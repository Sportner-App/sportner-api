namespace Sportner.API.Extensions.Cors;

public static class CorsExtensions
{
    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Dogrulama burada, AddCors'un disinda: policy lambda'si ilk istege
        // kadar calismadigi icin icine konan bir hata basta degil, trafik
        // basladiktan sonra patlardi.
        if (!environment.IsDevelopment())
        {
            var configured = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            if (configured.Any(origin => origin.Trim() == "*"))
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins '*' iceremez: asagidaki politika AllowCredentials "
                    + "kullaniyor, yani her siteye kimlik bilgisiyle erisim verilmis olurdu. "
                    + "Izin verilecek origin'leri tek tek yazin.");
            }
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (environment.IsDevelopment())
                {
                    // SignalR negotiate needs credentials-compatible CORS (AllowAnyOrigin cannot).
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
                else
                {
                    // Bos liste kasitli: mobil istemci CORS'a tabi degil, yani
                    // production'da izinli tarayici origin'i olmamasi normal.
                    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }
}
