using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace KanbanCord.Bot.EventHandlers;

public class GuildDownloadCompletedEventHandler : IEventHandler<GuildDownloadCompletedEventArgs>
{
    private readonly ILogger<GuildDownloadCompletedEventHandler> logger;

    public GuildDownloadCompletedEventHandler(ILogger<GuildDownloadCompletedEventHandler> logger)
    {
        this.logger = logger;
    }

    public Task HandleEventAsync(DiscordClient sender, GuildDownloadCompletedEventArgs eventArgs)
    {
        logger.LogInformation("Guild Download Completed: Downloaded {count} guilds.", eventArgs.Guilds.Count);

        return Task.CompletedTask;
    }
}