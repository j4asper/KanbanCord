using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using KanbanCord.Core.Interfaces;

namespace KanbanCord.Bot.Commands.Task;

[Command("task")]
[RequirePermissions(userPermissions: DiscordPermissions.ManageMessages, botPermissions: DiscordPermissions.None)]
partial class TaskCommandGroup
{
    private readonly ITaskItemRepository _taskItemRepository;

    public TaskCommandGroup(ITaskItemRepository taskItemRepository)
    {
        _taskItemRepository = taskItemRepository;
    }
}