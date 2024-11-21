using DSharpPlus;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KanbanCord.Bot;

class Program
{
    private static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddDiscordConfiguration()
            .AddServices()
            .AddDatabase()
            .BuildServiceProvider();
        
        // Create database collections if they are missing
        await DatabaseSetupHelper.CreateRequiredCollectionsAsync(
            serviceProvider.GetRequiredService<IMongoDatabase>(),
            serviceProvider.GetRequiredService<ILogger<Program>>());
        
        var discordClient = serviceProvider.GetRequiredService<DiscordClient>();

        DiscordActivity status = new("out for work", DiscordActivityType.Watching);
        
        discordClient.Logger.LogInformation($"Application Version: {EnvironmentHelpers.GetApplicationVersion()}");
        
        await discordClient.ConnectAsync(status, DiscordUserStatus.Online);

        discordClient.Logger.LogInformation($"Bot User: {discordClient.CurrentUser.Username} ({discordClient.CurrentUser.Id})");
        
        await Task.Delay(-1);
    }
}