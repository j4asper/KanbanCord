using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.Task;

public partial class TaskCommandGroup
{
    // Open to everyone, like the old commands
    [SubSlashCommand("user", "Displays all the tasks assigned to a specified user.")]
    public Task<InteractionMessageProperties> UserAsync(User user) => BuildAssignedTasksAsync(user.Id);

    [SubSlashCommand("me", "Displays all the tasks assigned to you.")]
    public Task<InteractionMessageProperties> MeAsync() => BuildAssignedTasksAsync(Context.User.Id);

    private async Task<InteractionMessageProperties> BuildAssignedTasksAsync(ulong assigneeId)
    {
        var columns = await _boardService.GetColumnsAsync(
            Context.Interaction.RequireGuildId(),
            [BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed],
            assigneeId);

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        return BoardView.Build(
            columns,
            id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser),
            "KanbanCord Board",
            BoardScope.All,
            subtitle: $"Tasks assigned to <@{assigneeId}>");
    }
}
