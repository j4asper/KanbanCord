using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace KanbanCord.DiscordApplication.EventHandlers;

public class GuildCreateEventHandler : IGuildCreateShardedGatewayHandler
{
    private readonly ILogger<GuildCreateEventHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public GuildCreateEventHandler(ILogger<GuildCreateEventHandler> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public ValueTask HandleAsync(GatewayClient client, GuildCreateEventArgs arg)
    {
        if (arg.Guild is null || arg.Guild.IsUnavailable || (_timeProvider.GetUtcNow() - arg.Guild.JoinedAt).TotalMinutes > 1)
            return ValueTask.CompletedTask;

        _logger.LogInformation("Joined guild: {GuildName} (ID: {GuildId})", arg.Guild.Name, arg.Guild.Id);
        
        return ValueTask.CompletedTask;
    }
}