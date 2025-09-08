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

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connections = await _discordClient.GetCurrentApplicationAsync();
            
            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy(exception: e);
        }
    }
}