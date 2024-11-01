using DSharpPlus.Commands;
using KanbanCord.Repositories;

namespace KanbanCord.Commands.Task;

[Command("task")]
partial class TaskCommandGroup
{
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly ISettingsRepository _settingsRepository;

    public TaskCommandGroup(ITaskItemRepository taskItemRepository, ISettingsRepository settingsRepository)
    {
        _taskItemRepository = taskItemRepository;
        _settingsRepository = settingsRepository;
    }
}