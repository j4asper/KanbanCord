using KanbanCord.Core.Repositories;

namespace KanbanCord.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly ITaskService _taskService;
    private readonly ISettingsRepository _settingsRepository;

    public SettingsService(ITaskService taskService, ISettingsRepository settingsRepository)
    {
        _taskService = taskService;
        _settingsRepository = settingsRepository;
    }

    public async Task RemoveGuildDataAsync(ulong guildId)
    {
        await _taskService.ResetBoardAsync(guildId);

        await _settingsRepository.RemoveAsync(guildId);
    }
}
