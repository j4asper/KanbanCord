using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Models;
using KanbanCord.Repositories;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("me")]
    [Description("Displays all the tasks assigned to you.")]
    public async ValueTask TaskMeCommand(SlashCommandContext context)
    {
        var boardItems = await _taskItemRepository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Board")
            .WithDescription($"Tasks assigned to {context.User.Mention}");
        
        var backlogString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Backlog, context.User.Id);
        embed.AddField("Backlog", backlogString);
        
        var inProgressString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.InProgress, context.User.Id);
        embed.AddField("In Progress", inProgressString);
        
        var compltedString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Completed, context.User.Id);
        embed.AddField("Completed", compltedString);

        await context.RespondAsync(embed);
    }
}