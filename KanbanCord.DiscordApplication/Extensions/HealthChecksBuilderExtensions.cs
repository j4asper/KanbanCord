using KanbanCord.DiscordApplication.HealthChecks;

namespace KanbanCord.DiscordApplication.Extensions;

public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddCustomHealthChecks(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<DiscordConnectivityHealthCheck>(nameof(DiscordConnectivityHealthCheck));
        
        return builder;
    }
}