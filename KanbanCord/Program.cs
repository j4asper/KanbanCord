using DSharpPlus;
using DSharpPlus.Entities;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KanbanCord;

class Program
{
    private static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddDiscordConfiguration()
            .AddServices()
            .AddDatabase()
            .BuildServiceProvider();
        
        var discordClient = serviceProvider.GetRequiredService<DiscordClient>();

        DiscordActivity status = new("out for work", DiscordActivityType.Watching);
        
        discordClient.Logger.LogInformation($"Application Version: {EnvironmentHelpers.GetApplicationVersion()}");
        
        await discordClient.ConnectAsync(status, DiscordUserStatus.Online);

        discordClient.Logger.LogInformation($"Bot User: {discordClient.CurrentUser.Username} ({discordClient.CurrentUser.Id})");
        
        await Task.Delay(-1);
    }
}