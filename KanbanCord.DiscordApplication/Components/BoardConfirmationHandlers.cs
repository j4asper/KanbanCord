using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Attributes;
using KanbanCord.DiscordApplication.Presentation;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

// Only the user who ran /clear or /reset may confirm or cancel it
[RequireInteractionOwner<ButtonInteractionContext>]
public class BoardConfirmationHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskService _taskService;

    public BoardConfirmationHandlers(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [ComponentInteraction(InteractionIds.ClearConfirm)]
    public async Task<InteractionCallbackProperties> ClearAsync()
    {
        await _taskService.ClearBoardAsync(Context.Interaction.RequireGuildId());

        return Replace("The board has been cleared.");
    }

    [ComponentInteraction(InteractionIds.ResetConfirm)]
    public async Task<InteractionCallbackProperties> ResetAsync()
    {
        await _taskService.ResetBoardAsync(Context.Interaction.RequireGuildId());

        return Replace("The board has been reset.");
    }

    [ComponentInteraction(InteractionIds.ClearCancel)]
    public Task<InteractionCallbackProperties> CancelClearAsync() => Task.FromResult(Replace("Clearing cancelled."));

    [ComponentInteraction(InteractionIds.ResetCancel)]
    public Task<InteractionCallbackProperties> CancelResetAsync() => Task.FromResult(Replace("Reset cancelled."));

    private static InteractionCallbackProperties Replace(string text)
    {
        var message = MessageFactory.Text(text);

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
