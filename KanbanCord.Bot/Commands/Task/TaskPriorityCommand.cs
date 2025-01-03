using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Bot.Providers;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Models;
using MongoDB.Bson;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("priority")]
    [Description("Set a priority for a task.")]
    public async ValueTask TaskPriorityCommand(
        SlashCommandContext context,
        [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task,
        [SlashChoiceProvider<PriorityChoiceProvider>] int priority)
    {
        var taskItem = await _taskItemRepository.GetTaskItemByObjectIdOrDefaultAsync(new ObjectId(task));

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor();
        
        if (taskItem is null)
        {
            embed.WithDescription("The selected task was not found, please try again.");
            
            await context.RespondAsync(embed);
            return;
        }
        
        if (taskItem.Priority == (Priority)priority)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"The selected task already has the priority **{((Priority)priority).ToString()}**.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }
        
        var fromPriority = taskItem.Priority;
        
        taskItem.Priority = (Priority)priority;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);
        
        embed.WithDescription(
                $"The priority for task \"{taskItem.Title}\" has been updated from **{fromPriority.ToString()}** to **{((Priority)priority).ToString()}**.");
        
        await context.RespondAsync(embed);
    }
}