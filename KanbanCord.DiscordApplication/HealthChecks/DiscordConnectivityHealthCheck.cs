using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetCord.Gateway;

namespace KanbanCord.DiscordApplication.HealthChecks;

public class DiscordConnectivityHealthCheck : IHealthCheck
{
    private readonly ShardedGatewayClient _shardedGatewayClient;

    public DiscordConnectivityHealthCheck(ShardedGatewayClient shardedGatewayClient)
    {
        _shardedGatewayClient = shardedGatewayClient;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var unhealthyShards = _shardedGatewayClient
            .Where(gatewayClient => gatewayClient.Status != WebSocketStatus.Ready)
            .ToList();
        
        if (unhealthyShards.Any())
        {
            var shardStrings = unhealthyShards.Select(x => $"Shard: {x.SequenceNumber}, Status: {x.Status}");
            
            return Task.FromResult(HealthCheckResult.Unhealthy($"Unhealthy Shards:\n{string.Join("\n", shardStrings)}"));
        }
        
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}