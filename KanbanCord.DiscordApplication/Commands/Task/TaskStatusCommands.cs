using KanbanCord.Core.Models;
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
    [SubSlashCommand("start", "Start a task and have it moved from Backlog to the In-Progress column.")]
    public async Task<InteractionMessageProperties> StartAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(BacklogTasksAutocompleteProvider))] string task)
    {
        var change = await _taskService.StartAsync(Context.Interaction.RequireGuildId(), task);

        var board = await _commandMentionService.GetMentionAsync("board");

        return MessageFactory.Text($"The task \"{change.Task.Title}\" has been started and moved to the **In Progress** column. View it using {board}.");
    }

    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("complete", "Complete a task and have it moved from In-Progress to the Completed column.")]
    public async Task<InteractionMessageProperties> CompleteAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(InProgressTasksAutocompleteProvider))] string task)
    {
        var change = await _taskService.CompleteAsync(Context.Interaction.RequireGuildId(), task);

        var board = await _commandMentionService.GetMentionAsync("board");

        return MessageFactory.Text($"The task \"{change.Task.Title}\" has been completed and moved to the **Completed** column. View it using {board}.");
    }

    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("archive", "Archive a task and have it moved to the list of archived items.")]
    public async Task<InteractionMessageProperties> ArchiveAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(CompletedTasksAutocompleteProvider))] string task)
    {
        var change = await _taskService.ArchiveAsync(Context.Interaction.RequireGuildId(), task);

        var archive = await _commandMentionService.GetMentionAsync("archive");

        return MessageFactory.Text(
            $"The task \"{change.Task.Title}\" has been moved from **{change.From.ToFormattedString()}** to **{BoardStatus.Archived.ToFormattedString()}**. View it using {archive}.");
    }

    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("move", "Move a task from one column to another.")]
    public async Task<InteractionMessageProperties> MoveAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task,
        ColumnChoice to)
    {
        var change = await _taskService.MoveAsync(Context.Interaction.RequireGuildId(), task, to.ToBoardStatus());

        return MessageFactory.Text(
            $"The task \"{change.Task.Title}\" has been moved from **{change.From.ToFormattedString()}** to **{to.ToBoardStatus().ToFormattedString()}**.");
    }
}
