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
    [Command("move")]
    [Description("Move a task from one column to another.")]
    public async ValueTask TaskMoveCommand(
        SlashCommandContext context,
        [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task,
        [SlashChoiceProvider<ColumnChoiceProvider>] int to)
    {
        var taskItem = await _taskItemRepository.GetTaskItemByObjectIdOrDefaultAsync(new ObjectId(task));
        
        if (taskItem is null)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    "The selected task was not found, please try again.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }
        
        if (taskItem.Status == (BoardStatus)to)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"The selected task is already in column **{((BoardStatus)to).ToFormattedString()}**.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }
        
        var fromColumn = taskItem.Status;
        
        taskItem.Status = (BoardStatus)to;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithDescription(
                $"The task \"{taskItem.Title}\" has been moved from **{fromColumn.ToFormattedString()}** to **{((BoardStatus)to).ToFormattedString()}**.");
        
        await context.RespondAsync(embed);
    }
}