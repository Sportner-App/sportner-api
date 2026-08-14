using Serilog;
using Sportner.API.Extensions.Authentication;
using Sportner.API.Extensions.Collection;
using Sportner.API.Extensions.Cors;
using Sportner.API.Extensions.HealthCheck;
using Sportner.API.Extensions.Localization;
using Sportner.API.Extensions.RateLimiting;
using Sportner.API.Extensions.Seeding;
using Sportner.API.Extensions.Swagger;
using Sportner.API.Hubs;
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
        .WriteTo.Console());

builder.Services.AddCustomCollection(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddCustomLocalization();
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddCustomRateLimiting();
// Overrides Application's NullChatRealtimeNotifier — must register after AddApplication.
builder.Services.AddSingleton<IChatRealtimeNotifier, SignalRChatRealtimeNotifier>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCustomLocalization();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseCustomSwagger();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ConversationHub>("/hubs/event-chat");
app.UseAppHealthChecks();

await app.SeedDatabaseAsync();
await app.SeedDemoDataAsync();

app.Run();

public partial class Program;
