using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;

namespace KanbanCord.Bot.Commands;

public class ArchiveCommand
{
    private readonly ITaskItemRepository _repository;

    public ArchiveCommand(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    

    [Command("archive")]
    [Description("Displays all the archived tasks.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var boardItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Archive");
        
        var archiveString = await boardItems.GetBoardTaskString(context.Client, BoardStatus.Archived);
        
        embed.WithDescription(archiveString);

        await context.RespondAsync(embed);
    }
}