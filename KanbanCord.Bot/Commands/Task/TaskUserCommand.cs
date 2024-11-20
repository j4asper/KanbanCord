using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("user")]
    [Description("Displays all the tasks assigned to a specified user.")]
    public async ValueTask TaskUserCommand(SlashCommandContext context, DiscordUser user)
    {
        var boardItems = await _taskItemRepository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Board")
            .WithDescription($"Tasks assigned to {user.Mention}");
        
        var backlogString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Backlog, user.Id);
        embed.AddField("Backlog", backlogString);
        
        var inProgressString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.InProgress, user.Id);
        embed.AddField("In Progress", inProgressString);
        
        var compltedString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Completed, user.Id);
        embed.AddField("Completed", compltedString);

        await context.RespondAsync(embed);
    }
}