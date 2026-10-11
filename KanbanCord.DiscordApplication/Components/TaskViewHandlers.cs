using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class TaskViewHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskService _taskService;
    private readonly IUserNameResolver _userNameResolver;

    public TaskViewHandlers(ITaskService taskService, IUserNameResolver userNameResolver)
    {
        _taskService = taskService;
        _userNameResolver = userNameResolver;
    }

    // Anyone may page through a task, matching the old allowUsageByAnyone: true.
    // The id is task-view-page:{taskId}:{page}:{b|f}; the b/f suffix only keeps the back and forward buttons' ids
    // unique (Discord rejects duplicates when both point at the same page) and is not used.
    [ComponentInteraction(InteractionIds.TaskViewPage)]
    public async Task<InteractionCallbackProperties> PageAsync(string taskId, int page, string direction)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), taskId);

        var names = await _userNameResolver.ResolveAsync(item.Comments.Select(c => c.AuthorId).Append(item.AuthorId));

        var message = TaskView.Build(item, names, page);

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
