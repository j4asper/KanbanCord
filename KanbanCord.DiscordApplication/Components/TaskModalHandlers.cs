using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class TaskModalHandlers : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ITaskService _taskService;
    private readonly ICommandMentionService _commandMentionService;

    public TaskModalHandlers(ITaskService taskService, ICommandMentionService commandMentionService)
    {
        _taskService = taskService;
        _commandMentionService = commandMentionService;
    }

    [ComponentInteraction(InteractionIds.TaskAddModal)]
    public async Task<InteractionCallbackProperties> AddAsync()
    {
        var task = await _taskService.AddAsync(
            Context.Interaction.RequireGuildId(),
            Context.User.Id,
            Context.Components.GetText(InteractionIds.TitleField),
            Context.Components.GetText(InteractionIds.DescriptionField),
            TaskModals.ParsePriority(Context.Components.GetSelectedValue(InteractionIds.PriorityField)));

        var board = await _commandMentionService.GetMentionAsync("board");

        return InteractionCallback.Message(MessageFactory.Text($"The task \"{task.Title}\" has been added to the backlog with **{task.Priority}** priority. View it using {board}."));
    }

    [ComponentInteraction(InteractionIds.TaskEditModal)]
    public async Task<InteractionCallbackProperties> EditAsync(string taskId)
    {
        var task = await _taskService.EditAsync(
            Context.Interaction.RequireGuildId(),
            taskId,
            Context.Components.GetText(InteractionIds.TitleField),
            Context.Components.GetText(InteractionIds.DescriptionField),
            // An unreadable selection keeps the task's current priority
            TaskModals.ParsePriorityOrNull(Context.Components.GetSelectedValue(InteractionIds.PriorityField)));

        var board = await _commandMentionService.GetMentionAsync("board");

        return InteractionCallback.Message(MessageFactory.Text($"The task \"{task.Title}\" has been edited. View it using {board}."));
    }

    [ComponentInteraction(InteractionIds.TaskCommentModal)]
    public async Task<InteractionCallbackProperties> CommentAsync(string taskId)
    {
        var task = await _taskService.AddCommentAsync(
            Context.Interaction.RequireGuildId(),
            taskId,
            Context.User.Id,
            Context.Components.GetText(InteractionIds.CommentField));

        var view = await _commandMentionService.GetMentionAsync("task", "view");

        return InteractionCallback.Message(MessageFactory.Text($"A comment has been added to task \"{task.Title}\". View it using {view}."));
    }
}
