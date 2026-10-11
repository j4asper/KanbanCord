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
    [SubSlashCommand("assign", "Assign a task to a user.")]
    public async Task<InteractionMessageProperties> AssignAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task,
        [SlashCommandParameter(Description = "The user to assign this task to, leave empty to remove assignee")] User? assignee = null)
    {
        var change = await _taskService.AssignAsync(Context.Interaction.RequireGuildId(), task, assignee?.Id);

        if (assignee is null)
            return MessageFactory.Text($"The task \"{change.Task.Title}\" is no longer assigned to <@{change.PreviousAssigneeId}>.");

        var directMessageSent = await TrySendAssignmentDirectMessageAsync(assignee, change.Task.Title, change.Task.Description);

        var message = MessageFactory.Text($"The task \"{change.Task.Title}\" has been assigned to <@{assignee.Id}>.");

        // Ping in the channel only when the assignee could not be reached by DM (their DMs are closed)
        return directMessageSent ? message : message.WithAllowedUsers(assignee.Id);
    }

    private async Task<bool> TrySendAssignmentDirectMessageAsync(User assignee, string title, string description)
    {
        try
        {
            var channel = await assignee.GetDMChannelAsync();

            await channel.SendMessageAsync(new MessageProperties
            {
                Components =
                [
                    new ComponentContainerProperties(
                    [
                        new TextDisplayProperties($"### You have been assigned to a Task!\n-# {Context.Guild?.Name ?? "a server"}"),
                        new TextDisplayProperties($"**Task Name**\n{title}\n\n**Task Description**\n{description}"),
                    ]).WithAccentColor(Colors.Default),
                ],
                Flags = MessageFlags.IsComponentsV2,
            });

            return true;
        }
        catch (Exception)
        {
            // Closed DMs / blocked bot: caller falls back to a channel ping
            return false;
        }
    }
}
