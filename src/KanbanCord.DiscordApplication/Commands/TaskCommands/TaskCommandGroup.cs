using KanbanCord.Core.Services;
using NetCord;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskCommands;

[SlashCommand("task", "Task Related Commands",
    Contexts = [InteractionContextType.Guild],
    DefaultGuildPermissions = Permissions.ManageMessages)]
public partial class TaskCommandGroup : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ITaskItemService _taskItemService;

    public TaskCommandGroup(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }
}