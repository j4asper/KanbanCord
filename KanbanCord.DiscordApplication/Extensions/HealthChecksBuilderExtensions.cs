using KanbanCord.DiscordApplication.HealthChecks;

namespace KanbanCord.DiscordApplication.Extensions;

public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddKanbanCordHealthChecks(this IHealthChecksBuilder builder)
    {
        builder
            .AddCheck<DiscordConnectivityHealthCheck>(nameof(DiscordConnectivityHealthCheck))
            .AddCheck<MongoDbConnectivityHealthCheck>(nameof(MongoDbConnectivityHealthCheck))
            ;
        
        return builder;
    }
}