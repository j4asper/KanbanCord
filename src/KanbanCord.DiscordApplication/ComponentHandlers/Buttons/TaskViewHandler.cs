using KanbanCord.Core.Constants;
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Extensions;
using MongoDB.Bson;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.ComponentHandlers.Buttons;

public class TaskViewHandler : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskItemService _taskItemService;

    public TaskViewHandler(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    [ComponentInteraction(InteractionIdConstants.Task.ViewButtonId)]
    public async Task<InteractionMessageProperties> InteractionHandler(ObjectId taskId)
    {
        var task = await _taskItemService.GetTaskItemByObjectIdOrDefaultAsync(taskId);

        if (task is null)
        {
            return new InteractionMessageProperties()
                .WithFlags(MessageFlags.IsComponentsV2)
                .WithComponents([
                    new ComponentContainerProperties([
                        new TextDisplayProperties("The requested task does not exist."),
                    ])
                    .WithDefaultColor()
                ]);
        }
        
        var author = await Context.Client.Rest.GetUserAsync(task.AuthorId);
        
        
        var assignedTo = task.AssigneeId is null
            ? "Not assigned"
            : (await Context.Client.Rest.GetUserAsync((ulong)task.AssigneeId)).ToString();
        
        return new InteractionMessageProperties()
            .WithFlags(MessageFlags.IsComponentsV2)
            .WithComponents([
                new ComponentContainerProperties([
                    new TextDisplayProperties("**Title:**"),
                    new TextDisplayProperties(task.Title),
                    new TextDisplayProperties("**Description:**"),
                    new TextDisplayProperties(task.Description),
                    new TextDisplayProperties("**Author:**"),
                    new TextDisplayProperties(author.ToString()),
                    new TextDisplayProperties("**Assigned To:**"),
                    new TextDisplayProperties(assignedTo),
                    new TextDisplayProperties("**Current Column:**"),
                    new TextDisplayProperties(task.Status.ToFormattedString()),
                    new TextDisplayProperties("**Priority:**"),
                    new TextDisplayProperties(task.Priority.ToString()),
                    new TextDisplayProperties("**Created At:**"),
                    new TextDisplayProperties(new Timestamp(task.CreatedAt, TimestampStyle.LongDateTime).ToString()),
                    new TextDisplayProperties("**Last Updated At:**"),
                    new TextDisplayProperties(new Timestamp(task.LastUpdatedAt, TimestampStyle.LongDateTime).ToString()),
                    new ComponentSeparatorProperties(),
                    new ActionRowProperties([
                        new ButtonProperties(
                            $"{InteractionIdConstants.TaskComment.AddCommentButtonId}:{task.Id}",
                            "Add comment",
                            EmojiProperties.Standard("💬"),
                            ButtonStyle.Secondary)
                    ])
                ])
                .WithDefaultColor()
            ]);
    }
}