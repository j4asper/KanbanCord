using System.ComponentModel;
using DSharpPlus;
using DSharpPlus.BetterPagination.Extensions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Bot.Providers;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Models;
using MongoDB.Bson;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("view")]
    [Description("View a tasks details such as description, author and comments.")]
    [RequirePermissions(userPermissions: [], botPermissions: [])]
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
            ? await context.Client.GetUserAsync(taskItem.AssigneeId.Value)
            : null;

        var priorityString = taskItem.Priority switch
        {
            Priority.Low => ":yellow_circle: Low",
            Priority.High => ":red_circle: High",
            _ => ":orange_circle: Medium"
        };
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor(author.Username, iconUrl: author.AvatarUrl)
            .AddField("Title:", taskItem.Title)
            .AddField("Description:", taskItem.Description)
            .AddField("Author:", author.Mention)
            .AddField("Assigned To:", assignee is not null ? assignee.Mention : "None")
            .AddField("Current Column:", taskItem.Status.ToFormattedString())
            .AddField("Priority:", priorityString)
            .AddField("Created At:", Formatter.Timestamp(taskItem.CreatedAt, TimestampFormat.LongDateTime))
            .AddField("Last Updated At:", Formatter.Timestamp(taskItem.LastUpdatedAt, TimestampFormat.LongDateTime));
        
        var addCommentButton = new DiscordButtonComponent(
            DiscordButtonStyle.Secondary,
            $"{taskItem.Id.ToString()}.add-comment",
            "Add Comment",
            false,
            new DiscordComponentEmoji(DiscordEmoji.FromName(context.Client, ":speech_balloon:")));
        
        if (!taskItem.Comments.Any())
        {
            var response = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddComponents(addCommentButton);
            
            await context.RespondAsync(response);
            return;
        }

        embed.WithFooter("View comments on the following pages");
        
        List<Page> pages = [new() { Embed = embed }];

        foreach (var comment in taskItem.Comments)
        {
            var commenter = await context.Client.GetUserAsync(comment.AuthorId);

            var commentEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithAuthor(commenter.Username, iconUrl: commenter.AvatarUrl)
                .WithDescription(comment.Text)
                .AddField("Comment Added:", Formatter.Timestamp(comment.CreatedAt, TimestampFormat.LongDateTime));
            
            pages.Add(new Page { Embed = commentEmbed });
        }

        await context.SendBetterPaginatedMessageAsync(pages, [addCommentButton]);
    }
}