using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Constants;
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

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
        if (eventArgs.Id == "board-refresh")
            await HandleRefreshButtonClicked(sender, eventArgs);
        
        if (eventArgs.Id.EndsWith(".add-comment"))
            await HandleCommentButtonClicked(sender, eventArgs);
    }

    private async Task HandleRefreshButtonClicked(DiscordClient sender, ComponentInteractionCreatedEventArgs eventArgs)
    {
        var boardItems = await _repository.GetAllTaskItemsByGuildIdAsync(eventArgs.Guild.Id);
        
        var embed = await BoardHelper.GetBoardEmbed(sender, boardItems);
        
        var hasChanged = false;

        foreach (var field in eventArgs.Message.Embeds[0].Fields!)
        {
            if (embed.Fields!.All(x => x.Value != field.Value))
                hasChanged = true;
        }

        if (!hasChanged)
        {
            var followupMessage = new DiscordInteractionResponseBuilder()
                .WithContent("Board already up to date.")
                .AsEphemeral();
            
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, followupMessage);
            return;
        }
        
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
    
    private async Task HandleCommentButtonClicked(DiscordClient sender, ComponentInteractionCreatedEventArgs eventArgs)
    {
        var taskId = eventArgs.Id.Split(".")[0];
        
        var taskItem = await _repository.GetTaskItemByObjectIdOrDefaultAsync(new ObjectId(taskId));
        
        if (taskItem is null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    "The selected task was not found, please try again.");
            
            await eventArgs.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed));
            
            return;
        }
        
        var modal = new DiscordInteractionResponseBuilder()
            .WithCustomId(Guid.NewGuid().ToString())
            .WithTitle("Add a comment to a task")
            .AddComponents(new DiscordTextInputComponent(
                "Comment:",
                "commentField",
                "Put your comment here",
                max_length: Limits.TaskCommentMaxLength,
                min_length: Limits.TaskCommentMinLength,
                style: DiscordTextInputStyle.Paragraph));
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        
        var interaction = sender.ServiceProvider.GetRequiredService<InteractivityExtension>();
        
        var response = await interaction.WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(5));
        
        if (!response.TimedOut)
        {
            var modalInteraction = response.Result.Values;
            
            taskItem.Comments.Add(new Comment
            {
                AuthorId = eventArgs.User.Id,
                Text = modalInteraction["commentField"]
            });
            
            await _repository.UpdateTaskItemAsync(taskItem);

            var commands = await sender.GetGlobalApplicationCommandsAsync();
            
            var embed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"A comment has been added to task \"{taskItem.Title}\". View it using {commands.GetMention(["task", "view"])}.");
            
            await response.Result.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed));
        }
    }
}