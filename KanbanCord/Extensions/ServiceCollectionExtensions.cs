using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity.Extensions;
using KanbanCord.EventHandlers;
using KanbanCord.Helpers;
using KanbanCord.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KanbanCord.Extensions;

public static class ServiceCollectionExtensions
{
    public static ServiceCollection AddServices(this ServiceCollection services)
    {
        services.AddLogging(x =>
        {
            x.AddSimpleConsole(options =>
            {
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "[MMM dd yyyy - HH:mm:ss UTC] ";
            });
        });
        
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        return services;
    }
    
    public static ServiceCollection AddDatabase(this ServiceCollection services)
    {
        services.AddScoped<IMongoDatabase>(_ =>
            new MongoClient(EnvironmentHelpers.GetDatabaseConnectionString())
                .GetDatabase(EnvironmentHelpers.GetDatabaseName())
        );
        
        return services;
    }
    
    public static ServiceCollection AddDiscordConfiguration(this ServiceCollection services)
    {
        const DiscordIntents intents = DiscordIntents.None;
        
        services.AddDiscordClient(EnvironmentHelpers.GetBotToken(), intents)
            .Configure<DiscordConfiguration>(discordConfiguration =>
            {
                discordConfiguration.GatewayCompressionLevel = GatewayCompressionLevel.None;
                discordConfiguration.LogUnknownAuditlogs = false;
                discordConfiguration.LogUnknownEvents = false;
                discordConfiguration.AlwaysCacheMembers = false;
            })
            .AddInteractivityExtension()
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
            .ConfigureEventHandlers(eventHandlingBuilder => eventHandlingBuilder.AddEventHandlers<GuildDeletedEventHandler>());
        
        return services;
    }
}