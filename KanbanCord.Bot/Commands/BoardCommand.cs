using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Interfaces;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Commands;

public class BoardCommand
{
    private readonly ITaskItemRepository _repository;

    public BoardCommand(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    

    [Command("board")]
    [Description("Displays the board and all tasks.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var boardItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Board");
        
        var backlogString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Backlog);
        embed.AddField("Backlog", backlogString);
        
        var inProgressString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.InProgress);
        embed.AddField("In Progress", inProgressString);
        
        var compltedString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Completed);
        embed.AddField("Completed", compltedString);

        await context.RespondAsync(embed);
    }
}