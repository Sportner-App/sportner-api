using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Behaviors;
using Sportner.Application.Common.Mapping;
using Sportner.Application.Features.Gamification;
using Sportner.Application.Features.Quests;
using Sportner.Application.Services.Recommendations;

namespace Sportner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.Configure<RecommendationOptions>(
            configuration.GetSection(RecommendationOptions.SectionName));
        services.AddScoped<IRecommendationService, RecommendationService>();

        services.AddScoped<IBadgeAwarder, BadgeAwarder>();
        services.AddScoped<IQuestProgressTracker, QuestProgressTracker>();
        services.AddScoped<IExpiredSessionCleaner, ExpiredSessionCleaner>();
        services.AddScoped<IEventReminderDispatcher, EventReminderDispatcher>();
        services.AddScoped<IEventCompletionDispatcher, EventCompletionDispatcher>();
        services.AddScoped<INotificationDeliveryDispatcher, NotificationDeliveryDispatcher>();
        // API overrides with SignalRChatRealtimeNotifier; workers/tests keep the no-op.
        services.AddSingleton<IChatRealtimeNotifier, NullChatRealtimeNotifier>();

        MappingConfig.Configure();
        services.AddMapster();

        return services;
    }
}
