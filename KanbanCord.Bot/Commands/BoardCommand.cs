using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Interfaces;

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
        
        var embed = await BoardHelper.GetBoardEmbed(context.Client, boardItems);
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource);
        
        var refreshButton = new DiscordButtonComponent(
            DiscordButtonStyle.Secondary,
            "board-refresh",
            "Refresh",
            false,
            new DiscordComponentEmoji(
                DiscordEmoji.FromName(context.Client, ":arrows_counterclockwise:"))
            );

        var responseBuilder = new DiscordWebhookBuilder()
            .AddEmbed(embed)
            .AddComponents(refreshButton);
        
        await context.Interaction.EditOriginalResponseAsync(responseBuilder);
    }
}