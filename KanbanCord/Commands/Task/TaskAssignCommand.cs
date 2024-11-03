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
    [Command("assign")]
    [Description("Assign a task to a user.")]
    public async ValueTask TaskAssignCommand(
        SlashCommandContext context,
        [SlashChoiceProvider<ColumnChoiceProvider>] int from,
        [Description("ID of the task")] [MinMaxValue(minValue: 1)] int id,
        [Description("The user to assign this task to")] DiscordUser assignee)
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

        taskItem.AssigneeId = assignee.Id;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithDescription(
                $"The task \"{taskItem.Title}\" has been assigned to {assignee.Mention}.");
        
        await context.RespondAsync(embed);
    }
}