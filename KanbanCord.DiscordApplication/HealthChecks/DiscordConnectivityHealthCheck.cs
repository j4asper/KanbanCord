using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.HealthChecks;

public class DiscordConnectivityHealthCheck : IHealthCheck
{
    private readonly RestClient _restClient;
    private readonly ILogger<DiscordConnectivityHealthCheck> _logger;

    public DiscordConnectivityHealthCheck(RestClient restClient, ILogger<DiscordConnectivityHealthCheck> logger)
    {
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _restClient.GetCurrentApplicationAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discord health check failed.");
            return HealthCheckResult.Unhealthy("Discord health check failed.", ex);
        }
    }
}
