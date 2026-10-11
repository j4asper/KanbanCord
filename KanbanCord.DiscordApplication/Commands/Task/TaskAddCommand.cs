using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.Task;

public partial class TaskCommandGroup
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("add", "Add a task to the backlog")]
    public Task<InteractionCallbackProperties> AddAsync() =>
        System.Threading.Tasks.Task.FromResult<InteractionCallbackProperties>(InteractionCallback.Modal(TaskModals.Add()));
}
