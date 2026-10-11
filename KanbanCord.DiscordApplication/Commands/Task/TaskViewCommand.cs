using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Providers;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.Task;

public partial class TaskCommandGroup
{
    // Open to everyone (the old command overrode the group's Manage Messages requirement)
    [SubSlashCommand("view", "View a tasks details such as description, author and comments.")]
    public async Task<InteractionMessageProperties> ViewAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);

        var names = await _userNameResolver.ResolveAsync(item.Comments.Select(c => c.AuthorId).Append(item.AuthorId));

        return TaskView.Build(item, names, 0);
    }
}
