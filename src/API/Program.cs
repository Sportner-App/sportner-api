using Serilog;
using Sportner.API.Extensions.Authentication;
using Sportner.API.Extensions.Collection;
using Sportner.API.Extensions.Cors;
using Sportner.API.Extensions.Forwarding;
using Sportner.API.Extensions.HealthCheck;
using Sportner.API.Extensions.Localization;
using Sportner.API.Extensions.RateLimiting;
using Sportner.API.Extensions.Seeding;
using Sportner.API.Extensions.Swagger;
using Sportner.API.BackgroundServices;
using Sportner.API.Hubs;
using Sportner.API.Logging;
using Sportner.API.Middleware;
using Sportner.API.Realtime;
using Sportner.Application;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        // SignalR token'ı query string'de geliyor; ham istek yolu loglandığı
        // için maskelenmeden kaydedilirdi.
        .Enrich.With<RedactQueryStringSecretsEnricher>()
        .WriteTo.Console());

builder.Services.AddCustomCollection(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
// Apple/Google token doğrulaması yalnızca burada yapılıyor; worker'lar bu
// yapılandırmayı taşımadığı için zorunluluk API'ye özel.
builder.Services.ValidateSocialAuthOptionsOnStart();
// Yerelde Resend anahtarsiz calisilabilir (kodlar loga yazilir); production'da
// anahtarsiz baslamak, kodlarin kimseye ulasmadigi sessiz bir ariza olurdu.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.ValidateEmailOptionsOnStart();
}
builder.Services.AddCustomAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCustomLocalization();
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddCustomForwardedHeaders();
builder.Services.AddCustomRateLimiting();
builder.Services.AddHostedService<ApiNotificationDeliveryService>();
// Overrides Application's NullChatRealtimeNotifier — must register after AddApplication.
builder.Services.AddSingleton<IChatRealtimeNotifier, SignalRChatRealtimeNotifier>();

var app = builder.Build();

// Proxy başlıklarını en başta çöz: aşağıdaki hiçbir middleware ham bağlantı
// adresini görmemeli (özellikle UseHttpsRedirection ve UseRateLimiter).
app.UseForwardedHeaders();

// Hata yanitlari da dahil her yanit kapsansin diye erkende.
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCustomLocalization();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// Swagger tüm uç noktaları, DTO'ları ve parametreleri açık eder; production'da
// kapalı. Gerekirse Swagger:Enabled=true ile geçici olarak açılabilir.
if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseCustomSwagger();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ConversationHub>("/hubs/event-chat");
app.UseAppHealthChecks();

await app.MigrateDatabaseAsync();
await app.SeedDatabaseAsync();
await app.SeedDemoDataAsync();

app.Run();

public partial class Program;
