using KanbanCord.Core.Options;
using KanbanCord.Core.Repositories;
using KanbanCord.DiscordApplication.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostDependencies(this IServiceCollection services)
    {
        services
            .AddServices()
            .AddOptions()
            .AddDiscordConfiguration()
            .AddSingleton(_ => TimeProvider.System)
            ;

        services.AddScoped<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            
            return new MongoClient(options.ConnectionString)
                .GetDatabase(options.Name);
        });
        
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        // services
        //     .AddHostedService<BotBackgroundService>()
        //     .AddHostedService<DatabaseSetupBackgroundService>()
        //     .AddHostedService<UptimeMonitorBackgroundService>()
        //     ;
        
        services
            .AddScoped<ITaskItemRepository, TaskItemRepository>()
            // .AddScoped<ISettingsRepository, SettingsRepository>()
            ;
        
        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.Database)
            .ValidateDataAnnotations();
        
        services.AddOptionsWithValidateOnStart<DiscordOptions>()
            .BindConfiguration(DiscordOptions.Discord)
            .ValidateDataAnnotations();
        
        services.AddOptionsWithValidateOnStart<UptimeMonitorOptions>()
            .BindConfiguration(UptimeMonitorOptions.UptimeMonitor)
            .ValidateDataAnnotations();

        return services;
    }

    private static IServiceCollection AddDiscordConfiguration(this IServiceCollection services)
    {
        services
            .AddDiscordShardedGateway(options =>
            {
                options.Intents = GatewayIntents.Guilds;

                options.Presence = new PresenceProperties(UserStatusType.Online)
                    .WithActivities([new UserActivityProperties("out for work", UserActivityType.Watching)]);
            })
            .AddApplicationCommands()
            .AddShardedGatewayHandlers(typeof(Program).Assembly)
            // .AddComponentInteractionHandlers()
            ;
        
        return services;
    }

}