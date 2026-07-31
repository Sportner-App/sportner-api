using Sportner.API.Extensions.Authentication;
using Sportner.API.Extensions.Collection;
using Sportner.API.Extensions.Cors;
using Sportner.API.Extensions.HealthCheck;
using Sportner.API.Extensions.Localization;
using Sportner.API.Extensions.RateLimiting;
using Sportner.API.Extensions.Swagger;
using Sportner.Application;
using Sportner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomCollection(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCustomLocalization();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddCustomRateLimiting();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseCustomLocalization();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseAppHealthChecks();

app.Run();

public partial class Program;
