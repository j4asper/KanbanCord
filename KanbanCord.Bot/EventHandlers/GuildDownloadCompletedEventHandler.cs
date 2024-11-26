using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace KanbanCord.Bot.EventHandlers;

public class GuildDownloadCompletedEventHandler : IEventHandler<GuildDownloadCompletedEventArgs>
{
    public Task HandleEventAsync(DiscordClient sender, GuildDownloadCompletedEventArgs eventArgs)
    {
        sender.Logger.LogInformation($"Guild Download Completed: Downloaded {eventArgs.Guilds.Count} guilds.");

        return Task.CompletedTask;
    }
}