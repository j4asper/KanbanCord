using DSharpPlus;
using DSharpPlus.EventArgs;
using KanbanCord.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KanbanCord.Bot.EventHandlers;

public class GuildDeletedEventHandler : IEventHandler<GuildDeletedEventArgs>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly ILogger<GuildDeletedEventHandler> logger;

    public GuildDeletedEventHandler(ISettingsRepository settingsRepository, ITaskItemRepository taskItemRepository, ILogger<GuildDeletedEventHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _taskItemRepository = taskItemRepository;
        this.logger = logger;
    }


    public async Task HandleEventAsync(DiscordClient sender, GuildDeletedEventArgs eventArgs)
    {
        if (eventArgs.Unavailable)
            return;
        
        await _taskItemRepository.RemoveAllTaskItemsByIdAsync(eventArgs.Guild.Id);
        
        await _settingsRepository.RemoveAsync(eventArgs.Guild.Id);
        
        logger.LogInformation("Left Guild: {guildName} ({guildId})", eventArgs.Guild.Name, eventArgs.Guild.Id);
    }
}