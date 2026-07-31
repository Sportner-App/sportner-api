using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sportner.Application.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Infrastructure.Options;
using Sportner.Infrastructure.Persistence;
using Sportner.Infrastructure.Services;

namespace Sportner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SupabaseSettings>(configuration.GetSection(SupabaseSettings.SectionName));

        services.AddDbContext<SportnerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SupabaseConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpClient<INotificationService, NotificationService>();
        services.AddHttpClient<IStorageService, SupabaseStorageService>();

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("SupabaseConnection")!)
            .AddDbContextCheck<SportnerDbContext>();

        return services;
    }
}
