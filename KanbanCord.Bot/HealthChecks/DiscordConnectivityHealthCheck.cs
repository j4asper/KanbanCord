using DSharpPlus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KanbanCord.Bot.HealthChecks;

public class DiscordConnectivityHealthCheck : IHealthCheck
{
    private DiscordClient _discordClient;

    public DiscordConnectivityHealthCheck(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connected = _discordClient.AllShardsConnected;
        
        return Task.FromResult(connected
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy());
    }
}