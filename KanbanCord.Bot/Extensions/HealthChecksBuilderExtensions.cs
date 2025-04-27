using KanbanCord.Bot.HealthChecks;

namespace KanbanCord.Bot.Extensions;

public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddKanbanCordHealthChecks(this IHealthChecksBuilder builder)
    {
        builder
            .AddCheck<DiscordConnectivityHealthCheck>(nameof(DiscordConnectivityHealthCheck))
            .AddCheck<MongoDbConnectivityHealthCheck>(nameof(MongoDbConnectivityHealthCheck));
        
        return builder;
    }
}