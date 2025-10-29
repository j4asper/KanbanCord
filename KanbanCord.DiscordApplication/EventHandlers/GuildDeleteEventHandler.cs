using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace KanbanCord.DiscordApplication.EventHandlers;

public class GuildDeleteEventHandler : IGuildDeleteShardedGatewayHandler
{
    private readonly ILogger<GuildDeleteEventHandler> _logger;

    public GuildDeleteEventHandler(ILogger<GuildDeleteEventHandler> logger)
    {
        _logger = logger;
    }
    
    public ValueTask HandleAsync(GatewayClient client, GuildDeleteEventArgs arg)
    {
        if (arg.IsUnavailable)
            return ValueTask.CompletedTask;
        
        _logger.LogInformation("Left guild: ID {GuildId}", arg.GuildId);
        
        return ValueTask.CompletedTask;
    }
}