using KanbanCord.Core.Constants;
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Extensions;
using MongoDB.Bson;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.ComponentHandlers.Modals;

public class TaskCommentHandler : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ITaskItemService _taskItemService;

    public TaskCommentHandler(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    [ComponentInteraction(InteractionIdConstants.TaskComment.AddCommentModalId)]
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
        
        var textInput = Context.Components
            .OfType<Label>()
            .Select(x => (TextInput)x.Component)
            .First(x => x.CustomId == InteractionIdConstants.TaskComment.AddCommentModalTextFieldId);

        var comment = new Comment
        {
            AuthorId = Context.User.Id,
            Text = textInput.Value
        };
        
        task.Comments.Add(comment);

        await _taskItemService.CreateOrUpdateTaskItemAsync(task);
        
        return new InteractionMessageProperties()
            .WithFlags(MessageFlags.IsComponentsV2)
            .WithComponents([
                new ComponentContainerProperties([
                    new TextDisplayProperties("The comment has been added to the task."),
                ])
                .WithDefaultColor()
            ]);
    }
}