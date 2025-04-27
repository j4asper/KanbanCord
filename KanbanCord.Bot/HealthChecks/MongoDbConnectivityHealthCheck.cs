using KanbanCord.Core.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace KanbanCord.Bot.HealthChecks;

public class MongoDbConnectivityHealthCheck: IHealthCheck
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbConnectivityHealthCheck> _logger;

    public MongoDbConnectivityHealthCheck(IMongoDatabase database, ILogger<MongoDbConnectivityHealthCheck> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            var collectionNames = await collections.ToListAsync(cancellationToken);

            var requiredCollections = Enum.GetValues<RequiredCollections>()
                .Select(x => x.ToString())
                .ToArray();

            foreach (var requiredCollection in requiredCollections)
            {
                if (!collectionNames.Contains(requiredCollection))
                    return HealthCheckResult.Unhealthy($"Collection '{requiredCollection}' does not exist.");
            }
            
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed.");
            return HealthCheckResult.Unhealthy("MongoDB health check failed.", ex);
        }
    }
}