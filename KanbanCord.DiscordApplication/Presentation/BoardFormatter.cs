using KanbanCord.Core.Services;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardFormatter
{
    public const string UnknownUser = "Unknown User";

    private const string Fence = "```bash\n";
    private const string Closing = "```";
    private const int OverflowLineReserve = 30; // room for "…and 1234 more"

    public static string FormatColumn(IReadOnlyList<BoardEntry> entries, Func<ulong, string> authorName, int maxLength)
    {
        if (entries.Count == 0)
            return $"{Fence} {Closing}";

        var lines = entries
            .Select(entry => $"{entry.Number} - \"{entry.Task.Title}\" added by: {authorName(entry.Task.AuthorId)}")
            .ToList();

        var budget = maxLength - Fence.Length - Closing.Length - OverflowLineReserve;

        var included = new List<string>();
        var used = 0;

        foreach (var line in lines)
        {
            var cost = line.Length + 1; // + newline

            if (used + cost > budget)
                break;

            included.Add(line);
            used += cost;
        }

        var hidden = lines.Count - included.Count;

        if (hidden > 0)
            included.Add($"…and {hidden} more");

        return $"{Fence}{string.Join('\n', included)}{Closing}";
    }
}
