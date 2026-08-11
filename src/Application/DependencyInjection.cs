using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Behaviors;
using Sportner.Application.Common.Mapping;
using Sportner.Application.Features.Gamification;

namespace Sportner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IBadgeAwarder, BadgeAwarder>();
        services.AddScoped<IExpiredSessionCleaner, ExpiredSessionCleaner>();
        services.AddScoped<IEventReminderDispatcher, EventReminderDispatcher>();
        services.AddScoped<INotificationDeliveryDispatcher, NotificationDeliveryDispatcher>();
        // API overrides with SignalRChatRealtimeNotifier; workers/tests keep the no-op.
        services.AddSingleton<IChatRealtimeNotifier, NullChatRealtimeNotifier>();

        MappingConfig.Configure();
        services.AddMapster();

        return services;
    }
}
