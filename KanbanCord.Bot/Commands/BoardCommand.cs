using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using KanbanCord.Bot.Helpers;
using KanbanCord.Bot.Providers;
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
    [Description("Displays a given column on the board.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context, [Description("The column to show")] [SlashChoiceProvider<ColumnChoiceProvider>] int column = 0)
    {
        var boardItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);

        var boardStatus = (BoardStatus)column;
        
        var embed = await BoardHelper.GetBoardEmbed(context.Client, boardItems, boardStatus);
        
        await context.RespondAsync(embed);
        
        // await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource);
        //
        // var refreshButton = new DiscordButtonComponent(
        //     DiscordButtonStyle.Secondary,
        //     "board-refresh",
        //     "Refresh",
        //     false,
        //     new DiscordComponentEmoji(
        //         DiscordEmoji.FromName(context.Client, ":arrows_counterclockwise:"))
        //     );
        //
        // var responseBuilder = new DiscordWebhookBuilder()
        //     .AddEmbed(embed)
        //     .AddComponents(refreshButton);
        //
        // await context.Interaction.EditOriginalResponseAsync(responseBuilder);
    }
}