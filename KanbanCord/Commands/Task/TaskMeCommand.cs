using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Models;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("me")]
    [Description("Displays all the tasks assigned to you.")]
    public async ValueTask TaskMeCommand(SlashCommandContext context) => await TaskUserCommand(context, context.User);
}