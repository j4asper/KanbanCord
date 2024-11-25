using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Interfaces;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Commands;

public class ClearCommand
{
    private readonly ITaskItemRepository _repository;

    public ClearCommand(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    
    
    [Command("clear")]
    [Description("Clear the kanban board completely, this will archive all current tasks.")]
    [RequirePermissions(userPermissions: [DiscordPermission.ManageMessages], botPermissions: [])]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var clearButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, Guid.NewGuid().ToString(), "Clear");
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("Clear Board")
            .WithDescription("Are you sure you want to clear the board?");
        
        var responseMessage = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddComponents(clearButton);
        
        await context.RespondAsync(responseMessage);
        
        var message = await context.Interaction.GetOriginalResponseAsync();

        var response = await message.WaitForButtonAsync();

        switch (response.TimedOut)
        {
            case false when response.Result.Id == clearButton.CustomId && response.Result.User.Id == context.User.Id:
            {
                var tasks = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);

                foreach (var task in tasks)
                {
                    task.Status = BoardStatus.Archived;

                    await _repository.UpdateTaskItemAsync(task);
                }
                
                var deletedEmbed = new DiscordEmbedBuilder()
                    .WithDefaultColor()
                    .WithDescription("The board has been cleared.");
            
                await response.Result.Interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(deletedEmbed));

                return;
            }
            case true:
            {
                clearButton.Disable();
            
                var timedOutMessage = new DiscordMessageBuilder()
                    .AddEmbed(embed)
                    .AddComponents(clearButton);
            
                await message.ModifyAsync(timedOutMessage);
                break;
            }
        }
    }
}