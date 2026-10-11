using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Components;
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class TaskView
{
    public static int PageCount(TaskItem task) => 1 + task.Comments.Count;

    /// <summary>Page 0 shows the task details; pages 1..N show one comment each.</summary>
    public static InteractionMessageProperties Build(TaskItem task, IReadOnlyDictionary<ulong, string> names, int page)
    {
        var pageCount = PageCount(task);
        var current = Math.Clamp(page, 0, pageCount - 1);
        
        List<IComponentContainerComponentProperties> components = [];

        if (current == 0)
        {
            var priority = task.Priority switch
            {
                Priority.Low => "🟡 Low",
                Priority.High => "🔴 High",
                _ => "🟠 Medium",
            };

            components.Add(new TextDisplayProperties($"### {task.Title}"));
            components.Add(new TextDisplayProperties(task.Description));
            components.Add(new ComponentSeparatorProperties());
            components.Add(new TextDisplayProperties($"**Author:**\n<@{task.AuthorId}>"));
            components.Add(new TextDisplayProperties($"**Assigned To:**\n{(task.AssigneeId is { } assignee ? $"<@{assignee}>" : "None")}"));
            components.Add(new TextDisplayProperties($"**Current Column:**\n{task.Status.ToFormattedString()}"));
            components.Add(new TextDisplayProperties($"**Priority:**\n{priority}"));
            components.Add(new TextDisplayProperties($"**Created At:**\n{new Timestamp(task.CreatedAt, TimestampStyle.LongDateTime)}"));
            components.Add(new TextDisplayProperties($"**Last Updated At:**\n{new Timestamp(task.LastUpdatedAt, TimestampStyle.LongDateTime)}"));

            if (task.Comments.Count > 0)
                components.Add(new TextDisplayProperties("-# View comments on the following pages"));
        }
        else
        {
            var comment = task.Comments[current - 1];

            components.Add(new TextDisplayProperties($"### Comment by <@{comment.AuthorId}>"));
            components.Add(new TextDisplayProperties(comment.Text));
            components.Add(new TextDisplayProperties($"-# Comment added {new Timestamp(comment.CreatedAt, TimestampStyle.LongDateTime)}"));
        }

        List<IActionRowComponentProperties> paginationButtons = [];

        if (pageCount > 1)
        {
            var back = current == 0 ? pageCount - 1 : current - 1;
            var forward = current == pageCount - 1 ? 0 : current + 1;

            paginationButtons.Add(new ButtonProperties($"{InteractionIds.TaskViewPage}:{task.Id}:{back}:b", EmojiProperties.Standard("◀️"), ButtonStyle.Primary));
            paginationButtons.Add(new ButtonProperties(InteractionIds.Noop, $"{current + 1}/{pageCount}", ButtonStyle.Secondary).WithDisabled());
            paginationButtons.Add(new ButtonProperties($"{InteractionIds.TaskViewPage}:{task.Id}:{forward}:f", EmojiProperties.Standard("▶️"), ButtonStyle.Primary));
        }
        
        // Discord rejects action rows without components, so only add the paging row when there is more than one page
        if (paginationButtons.Count > 0)
            components.Add(new ActionRowProperties(paginationButtons));

        components.Add(new ActionRowProperties([new ButtonProperties($"{InteractionIds.TaskCommentButton}:{task.Id}", "Add Comment", EmojiProperties.Standard("💬"), ButtonStyle.Secondary)]));

        return MessageFactory.Container(components);
    }
}
