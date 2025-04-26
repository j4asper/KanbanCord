using KanbanCord.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KanbanCord.Bot.BackgroundServices;

public class DatabaseSetupBackgroundService : BackgroundService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<DatabaseSetupBackgroundService> _logger;

    public DatabaseSetupBackgroundService(IMongoDatabase database, ILogger<DatabaseSetupBackgroundService> logger)
    {
        _database = database;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var requiredCollections = Enum.GetValues<RequiredCollections>()
            .Select(x => x.ToString())
            .ToArray();

        var cursor = await _database.ListCollectionNamesAsync(cancellationToken: stoppingToken);
        
        var collectionList = await cursor.ToListAsync<string>(cancellationToken: stoppingToken);

        List<string> createdCollections = [];
        
        foreach (var collectionName in requiredCollections)
        {
            if (collectionList.Contains(collectionName))
                continue;
            
            await _database.CreateCollectionAsync(collectionName, cancellationToken: stoppingToken);
            
            createdCollections.Add(collectionName);
        }
        
        if (createdCollections.Count != 0)
            _logger.LogInformation("Created missing mongodb collection(s): {collections}", string.Join(", ", createdCollections));
    }
}