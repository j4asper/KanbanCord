using KanbanCord.Helpers;
using KanbanCord.Models;
using MongoDB.Driver;

namespace KanbanCord.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<Settings> collection;

    public SettingsRepository(IMongoDatabase mongoDatabase)
    {
        collection = mongoDatabase.GetCollection<Settings>(RequiredCollections.Settings.ToString());
    }
    
    
    public async Task<Settings?> GetByIdOrDefaultAsync(ulong guildId)
    {
        var settings = await collection.Find(x => x.GuildId == guildId).FirstOrDefaultAsync();
        
        return settings;
    }

    public async Task AddAsync(Settings settings)
    {
        await collection.InsertOneAsync(settings);
    }

    public async Task UpdateAsync(Settings settings)
    {
        await collection.ReplaceOneAsync(x => x.GuildId == settings.GuildId, settings);
    }

    public async Task RemoveAsync(ulong guildId)
    {
        await collection.DeleteOneAsync(x => x.GuildId == guildId);
    }
}