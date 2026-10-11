using KanbanCord.Core.Options;
using KanbanCord.Core.Repositories;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.BackgroundServices;
using KanbanCord.DiscordApplication.Providers;
using KanbanCord.DiscordApplication.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostDependencies(this IServiceCollection services)
    {
        services
            .AddOptions()
            .AddMongoDatabase()
            .AddRepositories()
            .AddServices()
            .AddBackgroundServices()
            .AddDiscordConfiguration()
            .AddApplicationHealthChecks()
            ;

        services
            .AddHttpClient()
            .AddMemoryCache()
            .AddSingleton(TimeProvider.System)
            ;

        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services)
    {
        services
            .AddOptionsWithValidateOnStart<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.Database)
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<UptimeMonitorOptions>()
            .BindConfiguration(UptimeMonitorOptions.UptimeMonitor)
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<KanbanCordOptions>()
            .BindConfiguration(KanbanCordOptions.KanbanCord)
            .ValidateDataAnnotations();

        return services;
    }

    private static IServiceCollection AddMongoDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>()
                .GetDatabase(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.Name));

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<ITaskItemRepository, TaskItemRepository>()
            .AddScoped<ISettingsRepository, SettingsRepository>()
            ;

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services
            .AddScoped<ITaskService, TaskService>()
            .AddScoped<IBoardService, BoardService>()
            .AddScoped<ISettingsService, SettingsService>()
            ;

        services
            .AddScoped<IUserNameResolver, UserNameResolver>()
            .AddScoped<ICommandMentionService, CommandMentionService>()
            ;

        return services;
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services
            .AddHostedService<DatabaseSetupBackgroundService>()
            .AddHostedService<UptimeMonitorBackgroundService>()
            ;

        return services;
    }

    private static IServiceCollection AddDiscordConfiguration(this IServiceCollection services)
    {
        services
            .AddDiscordShardedGateway(options =>
            {
                options.Intents = GatewayIntents.Guilds;

                options.CacheProvider = new SlimGatewayClientCacheProvider();
                
                options.Presence = new PresenceProperties(UserStatusType.Online)
                    .WithActivities([new UserActivityProperties("Watching out for work", UserActivityType.Watching)]);
            })
            .AddApplicationCommands((options, sp) =>
            {
                options.ResultHandler = new Commands.ApplicationCommandResultHandler<ApplicationCommandContext>(sp.GetRequiredService<IOptions<KanbanCordOptions>>());
            })
            .AddShardedGatewayHandlers(typeof(Program).Assembly, ServiceLifetime.Scoped)
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>((options, sp) =>
            {
                options.ResultHandler = new Components.ComponentInteractionResultHandler<ButtonInteractionContext>(sp.GetRequiredService<IOptions<KanbanCordOptions>>());
            })
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>((options, sp) =>
            {
                options.ResultHandler = new Components.ComponentInteractionResultHandler<ModalInteractionContext>(sp.GetRequiredService<IOptions<KanbanCordOptions>>());
            })
            ;

        return services;
    }

    private static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddKanbanCordHealthChecks()
            ;

        return services;
    }
}
