using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using KanbanCord.Extensions;
using KanbanCord.Repositories;

namespace KanbanCord.Commands;

public class ResetCommand
{
    private readonly ITaskItemRepository _repository;

    public ResetCommand(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    
    
    [Command("reset")]
    [Description("Resets the kanban board completely, this will delete all current and archived tasks.")]
    [RequirePermissions(userPermissions: DiscordPermissions.ManageMessages, botPermissions: DiscordPermissions.None)]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var clearButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, Guid.NewGuid().ToString(), "Reset");
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("Reset Board")
            .WithDescription("Are you sure you want to reset the board? This can not be undone!");
        
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
                await _repository.RemoveAllTaskItemsByIdAsync(context.Guild!.Id);
            
                var deletedEmbed = new DiscordEmbedBuilder()
                    .WithDefaultColor()
                    .WithDescription("The board has been reset.");
            
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