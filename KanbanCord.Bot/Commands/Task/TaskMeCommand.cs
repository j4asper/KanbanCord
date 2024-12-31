using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("me")]
    [Description("Displays all the tasks assigned to you.")]
    [RequirePermissions(userPermissions: [], botPermissions: [])]
    public async ValueTask TaskMeCommand(SlashCommandContext context) => await TaskUserCommand(context, context.User);
}