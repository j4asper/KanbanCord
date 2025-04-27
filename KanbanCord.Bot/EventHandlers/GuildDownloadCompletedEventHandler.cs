using DSharpPlus;
using DSharpPlus.EventArgs;

namespace KanbanCord.Bot.EventHandlers;

public class GuildDownloadCompletedEventHandler : IEventHandler<GuildDownloadCompletedEventArgs>
{
    private readonly ILogger<GuildDownloadCompletedEventHandler> _logger;

    public GuildDownloadCompletedEventHandler(ILogger<GuildDownloadCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleEventAsync(DiscordClient sender, GuildDownloadCompletedEventArgs eventArgs)
    {
        _logger.LogInformation("Guild Download Completed: Downloaded {count} guilds.", eventArgs.Guilds.Count);

        return Task.CompletedTask;
    }
}