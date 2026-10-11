using KanbanCord.DiscordApplication.Components;
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
    [SubSlashCommand("delete", "Delete a task completely, this will skip the archive and never be accessible again.")]
    public async Task<InteractionMessageProperties> DeleteAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);

        var names = await _userNameResolver.ResolveAsync([item.AuthorId]);

        return MessageFactory.Container(
        [
            new TextDisplayProperties("### Delete Task"),
            new TextDisplayProperties("Are you sure you want to delete this task?"),
            new TextDisplayProperties($"**Title:** {item.Title}\n**Description:** {item.Description}\n**Author:** <@{item.AuthorId}> ({names.GetValueOrDefault(item.AuthorId, BoardFormatter.UnknownUser)})"),
            new ActionRowProperties(
            [
                new ButtonProperties($"{InteractionIds.TaskDeleteConfirm}:{item.Id}", "Delete", ButtonStyle.Danger),
                new ButtonProperties(InteractionIds.TaskDeleteCancel, "Cancel", ButtonStyle.Secondary),
            ]),
        ]);
    }
}
