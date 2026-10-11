using KanbanCord.Core.Models;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardScope
{
    public const string All = "all";

    private static readonly BoardStatus[] DefaultColumns = [BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed];

    public static IReadOnlyList<BoardStatus> Parse(string scope) =>
        Enum.TryParse<BoardStatus>(scope, out var status) && Enum.IsDefined(status) ? [status] : DefaultColumns;

    public static string ToScope(BoardStatus? status) => status?.ToString() ?? All;
}
