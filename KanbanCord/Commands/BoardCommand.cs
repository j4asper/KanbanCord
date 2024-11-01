using System.ComponentModel;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Extensions;
using KanbanCord.Models;
using KanbanCord.Repositories;

namespace KanbanCord.Commands;

public class BoardCommand
{
    private readonly ITaskItemRepository _repository;

    public BoardCommand(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    

    [Command("board")]
    [Description("Displays the board and all tasks")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var boardItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Board");
        
        var backlogString = await GetBoardTaskString(boardItems, context.Client, BoardStatus.Backlog);
        embed.AddField("Backlog", backlogString);
        
        var inProgressString = await GetBoardTaskString(boardItems, context.Client, BoardStatus.InProgress);
        embed.AddField("In Progress", inProgressString);
        
        var compltedString = await GetBoardTaskString(boardItems, context.Client, BoardStatus.Completed);
        embed.AddField("Completed", compltedString);

        await context.RespondAsync(embed);
    }

    private async Task<string> GetBoardTaskString(IReadOnlyList<TaskItem> boardItems, DiscordClient client, BoardStatus boardStatus)
    {
        List<string> taskStrings = [];
        
        var id = 1;
        
        foreach (var boardItem in boardItems.Where(x => x.Status == boardStatus))
        {
            var user = await client.GetUserAsync(boardItem.AuthorId);
            
            taskStrings.Add($"{id} - \"{boardItem.Title}\" added by: {user.Username}");
            
            id++;
        }
        
        return $"```{(taskStrings.Any() ? string.Join('\n', taskStrings) : " ")}```";
    }
}