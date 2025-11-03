using KanbanCord.Core.Constants;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Extensions;
using MongoDB.Bson;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.ComponentHandlers.Buttons;

public class TaskAddCommentHandler : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskItemService _taskItemService;

    public TaskAddCommentHandler(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    [ComponentInteraction(InteractionIdConstants.TaskComment.AddCommentButtonId)]
    public async Task<InteractionCallbackProperties> InteractionHandler(ObjectId taskId)
    {
        var task = await _taskItemService.GetTaskItemByObjectIdOrDefaultAsync(taskId);

        if (task is null)
        {
            return InteractionCallback.Message(new InteractionMessageProperties()
                .WithFlags(MessageFlags.IsComponentsV2)
                .WithComponents([
                    new ComponentContainerProperties([
                        new TextDisplayProperties("The requested task does not exist."),
                    ])
                    .WithDefaultColor()
                ])
            );
        }

        return InteractionCallback.Modal(new ModalProperties(
            $"{InteractionIdConstants.TaskComment.AddCommentModalId}:{task.Id}",
            "Add a comment to the task",
                [
                    new LabelProperties("Comment", new TextInputProperties(InteractionIdConstants.TaskComment.AddCommentModalTextFieldId, TextInputStyle.Paragraph))
                ]
            )
        );
    }
}