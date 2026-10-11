using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class BoardHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;

    public BoardHandlers(IBoardService boardService, IUserNameResolver userNameResolver)
    {
        _boardService = boardService;
        _userNameResolver = userNameResolver;
    }

    [ComponentInteraction(InteractionIds.BoardRefresh)]
    public async Task<InteractionCallbackProperties> RefreshAsync(string scope, string hash)
    {
        var columns = await _boardService.GetColumnsAsync(Context.Interaction.RequireGuildId(), BoardScope.Parse(scope));

        if (BoardHasher.Hash(columns) == hash)
            return InteractionCallback.Message(MessageFactory.Text("Board already up to date.", ephemeral: true));

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        var message = BoardView.Build(columns, id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser), "KanbanCord Board", scope, refreshable: true);

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
