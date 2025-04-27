using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;

namespace KanbanCord.Bot.BackgroundServices;

public class BotBackgroundService : IHostedService
{
    private readonly ILogger<BotBackgroundService> _logger;
    private readonly DiscordClient _discordClient;

    public BotBackgroundService(ILogger<BotBackgroundService> logger, DiscordClient discordClient)
    {
        _logger = logger;
        _discordClient = discordClient;
    }

    public async Task StartAsync(CancellationToken token)
    {
        DiscordActivity status = new("out for work", DiscordActivityType.Watching);
        
        await _discordClient.ConnectAsync(status, DiscordUserStatus.Online);
        
        _logger.LogInformation("Bot User: {username} ({userId})", _discordClient.CurrentUser.Username, _discordClient.CurrentUser.Id);
        _logger.LogInformation("Application Version: {version}", Assembly.GetExecutingAssembly().GetName().Version!.ToString());
    }

    public async Task StopAsync(CancellationToken token)
    {
        _logger.LogInformation("Shutting down bot...");
        await _discordClient.DisconnectAsync();
    }
}