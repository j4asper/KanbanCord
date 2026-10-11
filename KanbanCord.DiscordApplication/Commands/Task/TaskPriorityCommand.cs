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
    [SubSlashCommand("priority", "Set a priority for a task.")]
    public async Task<InteractionMessageProperties> PriorityAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task,
        PriorityChoice priority)
    {
        var change = await _taskService.SetPriorityAsync(Context.Interaction.RequireGuildId(), task, priority.ToPriority());

        return MessageFactory.Text(
            $"The priority for task \"{change.Task.Title}\" has been updated from **{change.From}** to **{priority.ToPriority()}**.");
    }
}
