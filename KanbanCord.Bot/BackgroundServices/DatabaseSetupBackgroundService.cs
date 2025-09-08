using KanbanCord.Core.Models;
using MongoDB.Driver;

namespace KanbanCord.Bot.BackgroundServices;

public class DatabaseSetupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DatabaseSetupBackgroundService> _logger;

    public DatabaseSetupBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<DatabaseSetupBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var requiredCollections = Enum.GetValues<RequiredCollections>()
            .Select(x => x.ToString())
            .ToArray();

        var database = _serviceScopeFactory
            .CreateScope().ServiceProvider
            .GetRequiredService<IMongoDatabase>();
        
        var cursor = await database.ListCollectionNamesAsync(cancellationToken: stoppingToken);
        
        var collectionList = await cursor.ToListAsync<string>(cancellationToken: stoppingToken);

        List<string> createdCollections = [];
        
        foreach (var collectionName in requiredCollections)
        {
            if (collectionList.Contains(collectionName))
                continue;
            
            await database.CreateCollectionAsync(collectionName, cancellationToken: stoppingToken);
            
            createdCollections.Add(collectionName);
        }
        
        if (createdCollections.Count != 0)
            _logger.LogInformation("Created missing mongodb collection(s): {collections}", string.Join(", ", createdCollections));
    }
}