using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Bot.Providers;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("comment")]
    [Description("Add a comment to a task")]
    public async ValueTask TaskCommentCommand(SlashCommandContext context, [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task)
    {
        var taskItem = await _taskItemRepository.GetTaskItemByObjectIdOrDefaultAsync(new ObjectId(task));
        
        if (taskItem is null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    "The selected task was not found, please try again.");
            
            await context.RespondAsync(embed);
            return;
        }
        
        var modal = new DiscordInteractionResponseBuilder()
            .WithCustomId(Guid.NewGuid().ToString())
            .WithTitle("Add a comment to a task")
            .AddComponents(new DiscordTextInputComponent(
                "Comment:",
                "commentField",
                "Put your comment here",
                max_length: 600,
                min_length: 10,
                style: DiscordTextInputStyle.Paragraph));
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        
        var interaction = context.Client.ServiceProvider.GetRequiredService<InteractivityExtension>();

        var response = await interaction.WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(5));
        
        if (!response.TimedOut)
        {
            var modalInteraction = response.Result.Values;
            
            taskItem.Comments.Add(new Comment
            {
                AuthorId = context.User.Id,
                Text = modalInteraction["commentField"]
            });
            
            await _taskItemRepository.UpdateTaskItemAsync(taskItem);

            var commands = await context.Client.GetGlobalApplicationCommandsAsync();
            
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