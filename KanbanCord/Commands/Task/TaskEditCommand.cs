using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Providers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("edit")]
    [Description("Edit a task title and or description.")]
    public async ValueTask TaskEditCommand(SlashCommandContext context, [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task)
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
        
        var modalId = Guid.NewGuid().ToString();
        
        var modal = new DiscordInteractionResponseBuilder()
            .WithCustomId(modalId)
            .WithTitle("Edit a Task")
            .AddComponents(new DiscordTextInputComponent(
                "Title:",
                "titleField",
                "Title of the task",
                taskItem.Title,
                max_length: 30))
            .AddComponents(new DiscordTextInputComponent(
                "Description:",
                "descriptionField",
                "Description of the task",
                taskItem.Description,
                max_length: 4000,
                style: DiscordTextInputStyle.Paragraph));
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        
        var interaction = context.Client.ServiceProvider.GetRequiredService<InteractivityExtension>();

        var response = await interaction.WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(10));
        
        if (!response.TimedOut)
        {
            var modalInteraction = response.Result.Values;
            
            taskItem.Title = modalInteraction["titleField"];
            taskItem.Description = modalInteraction["descriptionField"];
            taskItem.LastUpdatedAt = DateTime.UtcNow;
            
            await _taskItemRepository.UpdateTaskItemAsync(taskItem);

            var commands = await context.Client.GetGlobalApplicationCommandsAsync();
            
            var embed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"The task \"{taskItem.Title}\" has been edited. View it using {commands.GetMention(["board"])}.");
            
            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}