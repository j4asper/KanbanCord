using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using MongoDB.Driver;

namespace KanbanCord.DiscordApplication.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<Settings> _collection;

    public SettingsRepository(IMongoDatabase mongoDatabase)
    {
        _collection = mongoDatabase.GetCollection<Settings>(nameof(RequiredCollections.Settings));
    }
    
    public async Task<Settings?> GetSettingsByIdOrDefaultAsync(ulong guildId)
    {
        var filter = Builders<Settings>.Filter.Eq(x => x.GuildId, guildId);
        
        var settings = await _collection.Find(filter).FirstOrDefaultAsync();
        
        return settings;
    }

    public async Task CreateOrUpdateSettingsAsync(Settings settings)
    {
        var filter = Builders<Settings>.Filter.Eq(x => x.GuildId, settings.GuildId);

        await _collection.ReplaceOneAsync(
            filter,
            settings,
            new ReplaceOptions { IsUpsert = true }
        );
    }

    public async Task RemoveSettingsAsync(ulong guildId)
    {
        await _collection.DeleteOneAsync(x => x.GuildId == guildId);
    }
}