using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace KanbanCord.DiscordApplication.Events;

public class GuildCreatedEventHandler : IGuildCreateShardedGatewayHandler
{
    private readonly ILogger<GuildCreatedEventHandler> _logger;

    public GuildCreatedEventHandler(ILogger<GuildCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(GatewayClient client, GuildCreateEventArgs arg)
    {
        _logger.LogInformation("Guild available: {guildId}", arg.GuildId);

        return ValueTask.CompletedTask;
    }
}
