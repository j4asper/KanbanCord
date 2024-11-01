using DSharpPlus;
using DSharpPlus.EventArgs;
using KanbanCord.Repositories;

namespace KanbanCord.EventHandlers;

public class GuildDeletedEventHandler : IEventHandler<GuildDeletedEventArgs>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ITaskItemRepository _taskItemRepository;

    public GuildDeletedEventHandler(ISettingsRepository settingsRepository, ITaskItemRepository taskItemRepository)
    {
        _settingsRepository = settingsRepository;
        _taskItemRepository = taskItemRepository;
    }


    public async Task HandleEventAsync(DiscordClient sender, GuildDeletedEventArgs eventArgs)
    {
        if (eventArgs.Unavailable)
            return;
        
        await _taskItemRepository.RemoveAllTaskItemsByIdAsync(eventArgs.Guild.Id);
        
        await _settingsRepository.RemoveAsync(eventArgs.Guild.Id);
    }
}