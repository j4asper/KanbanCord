using System.ComponentModel;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Extensions;
using KanbanCord.Models;
using KanbanCord.Providers;
using MongoDB.Bson;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("view")]
    [Description("View a tasks details such as description, author and comments.")]
    [RequirePermissions(userPermissions: DiscordPermissions.None, botPermissions: DiscordPermissions.None)]
    public async ValueTask TaskViewCommand(SlashCommandContext context, [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task)
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
        
        var author = await context.Client.GetUserAsync(taskItem.AuthorId);
        
        var assignee = taskItem.AssigneeId is not null
            ? await context.Client.GetUserAsync(taskItem.AuthorId)
            : null;

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor(author.Username, iconUrl: author.AvatarUrl)
            .AddField("Title:", taskItem.Title)
            .AddField("Description:", taskItem.Description)
            .AddField("Author:", author.Mention)
            .AddField("Assigned To:", assignee is not null ? assignee.Mention : "None")
            .AddField("Current Column:", taskItem.Status.ToFormattedString())
            .AddField("Created At:", Formatter.Timestamp(taskItem.CreatedAt, TimestampFormat.LongDateTime))
            .AddField("Last Updated At:", Formatter.Timestamp(taskItem.LastUpdatedAt, TimestampFormat.LongDateTime));
        
        
        if (!taskItem.Comments.Any())
        {
            await context.RespondAsync(embed);
            return;
        }

        embed.WithFooter("View comments on the following pages");
        
        List<Page> pages = [new Page { Embed = embed }];

        foreach (var comment in taskItem.Comments)
        {
            var commentor = await context.Client.GetUserAsync(comment.AuthorId);

            var commentEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithAuthor(commentor.Username, iconUrl: commentor.AvatarUrl)
                .WithDescription(comment.Text)
                .AddField("Comment Added:", Formatter.Timestamp(comment.CreatedAt, TimestampFormat.LongDateTime));
            
            pages.Add(new Page { Embed = commentEmbed });
        }

        await context.SendSimplePaginatedMessage(pages);
    }
}