using DSharpPlus;
using DSharpPlus.EventArgs;

namespace KanbanCord.Bot.EventHandlers;

public class GuildCreatedEventHandler : IEventHandler<GuildCreatedEventArgs>
{
    private readonly ILogger<GuildCreatedEventHandler> _logger;

    public GuildCreatedEventHandler(ILogger<GuildCreatedEventHandler> logger)
    {
        _logger = logger;
    }


    public Task HandleEventAsync(DiscordClient sender, GuildCreatedEventArgs eventArgs)
    {
        _logger.LogInformation("Joined Guild: {guildName} ({guildId})", eventArgs.Guild.Name, eventArgs.Guild.Id);
        
        return Task.CompletedTask;
    }
}