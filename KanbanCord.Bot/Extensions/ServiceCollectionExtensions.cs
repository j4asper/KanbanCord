using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity.Extensions;
using KanbanCord.Bot.BackgroundServices;
using KanbanCord.Bot.EventHandlers;
using KanbanCord.Core.Options;
using KanbanCord.Core.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KanbanCord.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostDependencies(this IServiceCollection services)
    {
        services
            .AddServices()
            .AddOptions()
            .AddHttpClient()
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
        services
            .AddHostedService<BotBackgroundService>()
            .AddHostedService<DatabaseSetupBackgroundService>()
            .AddHostedService<UptimeMonitorBackgroundService>()
            ;
        
        services
            .AddScoped<ITaskItemRepository, TaskItemRepository>()
            .AddScoped<ISettingsRepository, SettingsRepository>()
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
    
    public static IServiceCollection AddDiscordConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        const DiscordIntents intents = DiscordIntents.None
                                       | DiscordIntents.Guilds;
        
        services
            .AddDiscordClient(configuration.GetRequiredSection("Discord:Token").Value!, intents)
            .Configure<DiscordConfiguration>(discordConfiguration =>
            {
                discordConfiguration.LogUnknownAuditlogs = false;
                discordConfiguration.LogUnknownEvents = false;
                discordConfiguration.AlwaysCacheMembers = false;
            })
            .AddInteractivityExtension()
            .UseZstdCompression()
            .AddCommandsExtension((_, extension) =>
            {
                extension.AddProcessor(new SlashCommandProcessor(new SlashCommandConfiguration()));
                extension.AddCommands(typeof(Program).Assembly);
            },
            new CommandsConfiguration
            {
                RegisterDefaultCommandProcessors = false,
                UseDefaultCommandErrorHandler = false
            })
            .ConfigureEventHandlers(eventHandlingBuilder =>
            {
                eventHandlingBuilder.AddEventHandlers<GuildDeletedEventHandler>();
                eventHandlingBuilder.AddEventHandlers<GuildCreatedEventHandler>();
                eventHandlingBuilder.AddEventHandlers<ComponentInteractionCreatedEventHandler>();
                eventHandlingBuilder.AddEventHandlers<GuildDownloadCompletedEventHandler>();
            });
        
        return services;
    }
}
