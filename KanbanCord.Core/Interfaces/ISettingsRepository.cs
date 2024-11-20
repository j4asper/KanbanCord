using KanbanCord.Core.Models;

namespace KanbanCord.Core.Interfaces;

public interface ISettingsRepository
{
    Task<Settings?> GetByIdOrDefaultAsync(ulong guildId);
    
    Task AddAsync(Settings settings);
    
    Task UpdateAsync(Settings settings);
    
    Task RemoveAsync(ulong guildId);
}