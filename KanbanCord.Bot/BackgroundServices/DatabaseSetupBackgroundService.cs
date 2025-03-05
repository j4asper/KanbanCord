using KanbanCord.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KanbanCord.Bot.BackgroundServices;

public class DatabaseSetupBackgroundService : IHostedService
{
    private readonly IMongoDatabase database;
    private readonly ILogger<DatabaseSetupBackgroundService> logger;

    public DatabaseSetupBackgroundService(IMongoDatabase database, ILogger<DatabaseSetupBackgroundService> logger)
    {
        this.database = database;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var requiredCollections = Enum.GetValues<RequiredCollections>()
            .Select(x => x.ToString())
            .ToArray();

        var cursor = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        
        var collectionList = await cursor.ToListAsync<string>(cancellationToken: cancellationToken);

        List<string> createdCollections = [];
        
        foreach (var collectionName in requiredCollections)
        {
            if (collectionList.Contains(collectionName))
                continue;
            
            await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
            
            createdCollections.Add(collectionName);
        }
        
        if (createdCollections.Count != 0)
            logger.LogInformation("Created missing mongodb collection(s): {collections}", string.Join(", ", createdCollections));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}