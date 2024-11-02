using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Models;
using KanbanCord.Providers;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("move")]
    [Description("Move a task from one column to another.")]
    public async ValueTask TaskMoveCommand(
        SlashCommandContext context,
        [SlashChoiceProvider<ColumnChoiceProvider>] int from,
        [Description("ID of the task to move")] [MinMaxValue(minValue: 1)] int id,
        [SlashChoiceProvider<ColumnChoiceProvider>] int to)
    {
        var taskItems = await _taskItemRepository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var taskItem = taskItems.GetTaskItemByIdOrDefault((BoardStatus)from, id);
        
        if (taskItem is null)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"A task with the given ID `{id}` was not found.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }

        taskItem.Status = (BoardStatus)to;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithDescription(
                $"The task has been moved from **{((BoardStatus)from).ToFormattedString()}** to **{((BoardStatus)to).ToFormattedString()}**.");
        
        await context.RespondAsync(embed);
    }
}