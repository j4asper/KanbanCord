using KanbanCord.Core.Services;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace KanbanCord.DiscordApplication.Events;

public class GuildDeletedEventHandler : IGuildDeleteShardedGatewayHandler
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<GuildDeletedEventHandler> _logger;

    public GuildDeletedEventHandler(ISettingsService settingsService, ILogger<GuildDeletedEventHandler> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async ValueTask HandleAsync(GatewayClient client, GuildDeleteEventArgs arg)
    {
        // An outage makes guilds temporarily unavailable; only a real removal may delete data
        if (arg.IsUnavailable)
        {
            _logger.LogInformation("Guild became unavailable: {guildId}", arg.GuildId);
            return;
        }

        await _settingsService.RemoveGuildDataAsync(arg.GuildId);

        _logger.LogInformation("Left guild: {guildId}", arg.GuildId);
    }
}
