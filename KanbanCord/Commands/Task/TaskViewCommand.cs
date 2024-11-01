using System.ComponentModel;
using DSharpPlus;
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
    [Command("view")]
    [Description("View a tasks details such as description, author and comments.")]
    public async ValueTask TaskViewCommand(SlashCommandContext context, [SlashChoiceProvider<ColumnChoiceProvider>] int column, [Description("ID of the task to view")] [MinMaxValue(minValue: 1)] int id)
    {
        var taskItems = await _taskItemRepository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);
        
        var taskItem = taskItems.GetTaskItemByIdOrDefault((BoardStatus)column, id);
        
        if (taskItem is null)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"A task with the given ID `{id}` was not found.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }
        
        var author = await context.Client.GetUserAsync(taskItem.AuthorId);

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor(author.Username, iconUrl: author.AvatarUrl)
            .AddField("Title:", taskItem.Title)
            .AddField("Description:", taskItem.Description)
            .AddField("Author:", author.Mention)
            .AddField("Current Column:", taskItem.Status.ToFormattedString())
            .AddField("Created At:", Formatter.Timestamp(taskItem.CreatedAt, TimestampFormat.LongDateTime))
            .AddField("Last Updated At:", Formatter.Timestamp(taskItem.LastUpdatedAt, TimestampFormat.LongDateTime));
        await context.RespondAsync(embed);
    }
}