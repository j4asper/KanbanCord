namespace KanbanCord.Core.Services;

public interface ISettingsService
{
    Task RemoveGuildDataAsync(ulong guildId);
}
