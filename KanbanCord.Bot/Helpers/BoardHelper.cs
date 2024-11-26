using DSharpPlus;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Helpers;

public static class BoardHelper
{
    public static async Task<DiscordEmbed> GetBoardEmbed(DiscordClient client, IReadOnlyList<TaskItem> boardItems)
    {
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Board");
        
        var backlogString = await boardItems.GetBoardTaskString(client, BoardStatus.Backlog);
        embed.AddField("Backlog", backlogString);
        
        var inProgressString = await boardItems.GetBoardTaskString(client, BoardStatus.InProgress);
        embed.AddField("In Progress", inProgressString);
        
        var compltedString = await boardItems.GetBoardTaskString(client, BoardStatus.Completed);
        embed.AddField("Completed", compltedString);
        
        return embed;
    }
}