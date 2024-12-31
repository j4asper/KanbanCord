using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Bot.Providers;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Models;
using MongoDB.Bson;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("archive")]
    [Description("Archive a task and have it moved to the list of archived items.")]
    public async ValueTask TaskArchiveCommand(SlashCommandContext context, [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task)
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
        
        var fromColumn = taskItem.Status;
        
        taskItem.Status = BoardStatus.Archived;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);

        var commands = await context.Client.GetGlobalApplicationCommandsAsync();
        
        embed.WithDescription(
                $"The task \"{taskItem.Title}\" has been moved from **{fromColumn.ToFormattedString()}** to **{BoardStatus.Archived.ToFormattedString()}**. View it using {commands.GetMention(["archive"])}.");
        
        await context.RespondAsync(embed);
    }
}