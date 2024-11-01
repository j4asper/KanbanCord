using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Models;
using KanbanCord.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("complete")]
    [Description("Complete a task and have it moved to the Completed column.")]
    public async ValueTask TaskCompleteCommand(SlashCommandContext context, [Description("ID of the task to complete")] [MinMaxValue(minValue: 1)] int id)
    {
        var taskItems = await _taskItemRepository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var taskItem = taskItems.GetTaskItemByIdOrDefault(BoardStatus.Backlog, id);
        
        if (taskItem is null)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"A task with the given ID `{id}` was not found.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }

        taskItem.Status = BoardStatus.Completed;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);

        var commands = await context.Client.GetGlobalApplicationCommandsAsync();
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithDescription(
                $"The task has been completed and moved to the Completed column. View it using {commands.GetMention(["board"])}.");
        
        await context.RespondAsync(embed);
    }
}