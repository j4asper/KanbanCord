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
    [SubSlashCommand("comment", "Add a comment to a task")]
    public async Task<InteractionCallbackProperties> CommentAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task)
    {
        // Validates the task exists in this guild before opening the modal, as the old command did
        await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);

        return InteractionCallback.Modal(TaskModals.Comment(task));
    }
}
