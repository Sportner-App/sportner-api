using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sportner.Application.Services;
using Sportner.Application.Validators;

namespace Sportner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ISportService, SportService>();

        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        return services;
    }
}
