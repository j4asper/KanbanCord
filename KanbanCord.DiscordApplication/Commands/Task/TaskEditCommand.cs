using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Providers;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.Task;

public partial class TaskCommandGroup
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("edit", "Edit a task title and or description.")]
    public async Task<InteractionCallbackProperties> EditAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);

        return InteractionCallback.Modal(TaskModals.Edit(item));
    }
}
