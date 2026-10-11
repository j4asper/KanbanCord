using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Attributes;
using KanbanCord.DiscordApplication.Presentation;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class TaskButtonHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskService _taskService;

    public TaskButtonHandlers(ITaskService taskService)
    {
        _taskService = taskService;
    }

    // Open to everyone, like the old "Add Comment" button on /task view
    [ComponentInteraction(InteractionIds.TaskCommentButton)]
    public async Task<InteractionCallbackProperties> CommentAsync(string taskId)
    {
        await _taskService.GetAsync(Context.Interaction.RequireGuildId(), taskId);

        return InteractionCallback.Modal(TaskModals.Comment(taskId));
    }

    [RequireInteractionOwner<ButtonInteractionContext>]
    [ComponentInteraction(InteractionIds.TaskDeleteConfirm)]
    public async Task<InteractionCallbackProperties> DeleteConfirmAsync(string taskId)
    {
        var task = await _taskService.DeleteAsync(Context.Interaction.RequireGuildId(), taskId);

        var message = MessageFactory.Text($"The task \"{task.Title}\" has been deleted.");

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }

    [RequireInteractionOwner<ButtonInteractionContext>]
    [ComponentInteraction(InteractionIds.TaskDeleteCancel)]
    public Task<InteractionCallbackProperties> DeleteCancelAsync()
    {
        var message = MessageFactory.Text("Deletion cancelled.");

        return Task.FromResult<InteractionCallbackProperties>(InteractionCallback.ModifyMessage(options => options.Components = message.Components));
    }
}
