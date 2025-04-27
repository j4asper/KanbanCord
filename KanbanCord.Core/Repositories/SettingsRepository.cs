using KanbanCord.Core.Models;
using MongoDB.Driver;

namespace KanbanCord.Core.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<Settings> _collection;

    public SettingsRepository(IMongoDatabase mongoDatabase)
    {
        _collection = mongoDatabase.GetCollection<Settings>(nameof(RequiredCollections.Settings));
    }
    
    
    public async Task<Settings?> GetByIdOrDefaultAsync(ulong guildId)
    {
        var settings = await _collection.Find(x => x.GuildId == guildId).FirstOrDefaultAsync();
        
        return settings;
    }

    public async Task AddAsync(Settings settings)
    {
        await _collection.InsertOneAsync(settings);
    }

    public async Task UpdateAsync(Settings settings)
    {
        await _collection.ReplaceOneAsync(x => x.GuildId == settings.GuildId, settings);
    }

    public async Task RemoveAsync(ulong guildId)
    {
        await _collection.DeleteOneAsync(x => x.GuildId == guildId);
    }
}