using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Services;
using NetCord;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.Task;

[SlashCommand("task", "Task related commands", Contexts = [InteractionContextType.Guild])]
public partial class TaskCommandGroup : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ITaskService _taskService;
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;
    private readonly ICommandMentionService _commandMentionService;

    public TaskCommandGroup(
        ITaskService taskService,
        IBoardService boardService,
        IUserNameResolver userNameResolver,
        ICommandMentionService commandMentionService)
    {
        _taskService = taskService;
        _boardService = boardService;
        _userNameResolver = userNameResolver;
        _commandMentionService = commandMentionService;
    }
}
