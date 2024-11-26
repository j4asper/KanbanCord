using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Interfaces;

namespace KanbanCord.Bot.EventHandlers;

public class ComponentInteractionCreatedEventHandler  :IEventHandler<ComponentInteractionCreatedEventArgs>
{
    private readonly ITaskItemRepository _repository;

    public ComponentInteractionCreatedEventHandler(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    
    
    public async Task HandleEventAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs eventArgs)
    {
        if (eventArgs.Id != "refresh")
            return;
        
        var boardItems = await _repository.GetAllTaskItemsByGuildIdAsync(eventArgs.Guild.Id);
        
        var embed = await BoardHelper.GetBoardEmbed(sender, boardItems);
        
        var hasChanged = false;

        foreach (var field in eventArgs.Message.Embeds[0].Fields!)
        {
            if (embed.Fields!.All(x => x.Value != field.Value))
                hasChanged = true;
        }

        if (!hasChanged)
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
        
        var refreshButton = new DiscordButtonComponent(
            DiscordButtonStyle.Secondary,
            "refresh",
            "Refresh",
            false,
            new DiscordComponentEmoji(
                DiscordEmoji.FromName(sender, ":arrows_counterclockwise:"))
        );
        
        var response = new DiscordInteractionResponseBuilder()
            .AddEmbed(embed)
            .AddComponents(refreshButton);

        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, response);
    }
}