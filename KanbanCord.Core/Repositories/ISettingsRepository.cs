using KanbanCord.Core.Models;

namespace KanbanCord.Core.Repositories;

public interface ISettingsRepository
{
    Task<Settings?> GetSettingsByIdOrDefaultAsync(ulong guildId);
    
    Task CreateOrUpdateSettingsAsync(Settings settings);
    
    Task RemoveSettingsAsync(ulong guildId);
}