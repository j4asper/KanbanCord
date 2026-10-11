using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class ArchiveCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;

    public ArchiveCommand(IBoardService boardService, IUserNameResolver userNameResolver)
    {
        _boardService = boardService;
        _userNameResolver = userNameResolver;
    }

    [SlashCommand("archive", "Displays all the archived tasks.", Contexts = [InteractionContextType.Guild])]
    public async Task<InteractionMessageProperties> ArchiveAsync()
    {
        var columns = await _boardService.GetColumnsAsync(Context.Interaction.RequireGuildId(), [BoardStatus.Archived]);

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        return BoardView.Build(
            columns,
            id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser),
            "KanbanCord Archive",
            BoardScope.ToScope(BoardStatus.Archived));
    }
}
