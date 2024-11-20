using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KanbanCord.Core.Helpers;

public static class DatabaseSetupHelper
{
    public static async Task CreateRequiredCollectionsAsync(IMongoDatabase database, ILogger logger)
    {
        var requiredCollections = Enum.GetValues(typeof(RequiredCollections))
            .Cast<RequiredCollections>()
            .Select(x => x.ToString())
            .ToArray();
        
        var collectionList = await (await database.ListCollectionNamesAsync()).ToListAsync<string>();

        List<string> createdCollections = [];
        
        foreach (var collectionName in requiredCollections)
        {
            if (collectionList.Contains(collectionName))
                continue;
            
            await database.CreateCollectionAsync(collectionName);
            
            createdCollections.Add(collectionName);
        }
        
        logger.LogInformation($"Created missing mongodb collection(s): {string.Join(", ", createdCollections)}");
    }
}

public enum RequiredCollections
{
    Tasks,
    Settings
}
