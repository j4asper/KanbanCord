using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KanbanCord.Helpers;

public static class DatabaseSetupHelper
{
    public static async Task CreateRequiredCollectionsAsync(IServiceProvider serviceProvider)
    {
        var database = serviceProvider.GetRequiredService<IMongoDatabase>();
        
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        var requiredCollections = Enum.GetValues(typeof(RequiredCollections))
            .Cast<RequiredCollections>()
            .Select(x => x.ToString())
            .ToArray();
        
        var collectionList = await (await database.ListCollectionNamesAsync()).ToListAsync();
        
        foreach (var collectionName in requiredCollections)
        {
            if (collectionList.Contains(collectionName))
                continue;
            
            await database.CreateCollectionAsync(collectionName);
            logger.LogInformation($"Created missing mongodb collection: {collectionName}");
        }
    }
}

public enum RequiredCollections
{
    Tasks,
    Settings
}
