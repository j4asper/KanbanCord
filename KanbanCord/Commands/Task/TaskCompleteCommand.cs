using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Models;
using KanbanCord.Providers;
using MongoDB.Bson;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("complete")]
    [Description("Complete a task and have it moved from In-Progress to the Completed column.")]
    public async ValueTask TaskCompleteCommand(SlashCommandContext context, [Description("Search for the task to select")] [SlashAutoCompleteProvider<InProgressTaskItemsAutoCompleteProvider>] string task)
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

        taskItem.Status = BoardStatus.Completed;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);

        var commands = await context.Client.GetGlobalApplicationCommandsAsync();
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithDescription(
                $"The task \"{taskItem.Title}\" has been completed and moved to the **Completed** column. View it using {commands.GetMention(["board"])}.");
        
        await context.RespondAsync(embed);
    }
}