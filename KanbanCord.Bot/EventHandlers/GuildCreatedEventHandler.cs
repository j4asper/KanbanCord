using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace KanbanCord.Bot.EventHandlers;

public class GuildCreatedEventHandler : IEventHandler<GuildCreatedEventArgs>
{
    private readonly ILogger<GuildCreatedEventHandler> logger;

    public GuildCreatedEventHandler(ILogger<GuildCreatedEventHandler> logger)
    {
        this.logger = logger;
    }


    public Task HandleEventAsync(DiscordClient sender, GuildCreatedEventArgs eventArgs)
    {
        logger.LogInformation("Joined Guild: {guildName} ({guildId})", eventArgs.Guild.Name, eventArgs.Guild.Id);
        
        return Task.CompletedTask;
    }
}