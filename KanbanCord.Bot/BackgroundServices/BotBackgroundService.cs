using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KanbanCord.Bot.BackgroundServices;

public class BotBackgroundService : IHostedService
{
    private readonly ILogger<BotBackgroundService> logger;
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly DiscordClient discordClient;

    public BotBackgroundService(ILogger<BotBackgroundService> logger, IHostApplicationLifetime applicationLifetime, DiscordClient discordClient)
    {
        this.logger = logger;
        this.applicationLifetime = applicationLifetime;
        this.discordClient = discordClient;
    }

    public async Task StartAsync(CancellationToken token)
    {
        DiscordActivity status = new("out for work", DiscordActivityType.Watching);
        
        await discordClient.ConnectAsync(status, DiscordUserStatus.Online);
        
        logger.LogInformation("Bot User: {username} ({userId})", discordClient.CurrentUser.Username, discordClient.CurrentUser.Id);
        logger.LogInformation("Application Version: {version}", Assembly.GetExecutingAssembly().GetName().Version!.ToString());
    }

    public async Task StopAsync(CancellationToken token)
    {
        logger.LogInformation("Shutting down bot...");
        await discordClient.DisconnectAsync();
    }
}