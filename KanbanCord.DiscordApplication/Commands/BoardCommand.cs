using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class BoardCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;

    public BoardCommand(IBoardService boardService, IUserNameResolver userNameResolver)
    {
        _boardService = boardService;
        _userNameResolver = userNameResolver;
    }

    [SlashCommand("board", "Displays a given column on the board.", Contexts = [InteractionContextType.Guild])]
    public async Task<InteractionMessageProperties> BoardAsync(
        [SlashCommandParameter(Description = "The column to show")] ColumnChoice? column = null)
    {
        var scope = BoardScope.ToScope(column?.ToBoardStatus());

        var columns = await _boardService.GetColumnsAsync(Context.Interaction.RequireGuildId(), BoardScope.Parse(scope));

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        return BoardView.Build(
            columns,
            id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser),
            "KanbanCord Board",
            scope,
            refreshable: true);
    }
}
