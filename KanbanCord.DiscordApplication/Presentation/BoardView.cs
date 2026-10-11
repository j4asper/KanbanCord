using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Components;
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardView
{
    public const int MultiColumnMaxLength = 1200;
    public const int SingleColumnMaxLength = 3500;

    public static InteractionMessageProperties Build(
        IReadOnlyList<BoardColumn> columns,
        Func<ulong, string> authorName,
        string title,
        string scope,
        string? subtitle = null,
        bool refreshable = false)
    {
        List<IComponentContainerComponentProperties> components = [new TextDisplayProperties($"### {title}")];

        if (subtitle is not null)
            components.Add(new TextDisplayProperties(subtitle));

        var single = columns.Count == 1;

        foreach (var column in columns)
        {
            var block = BoardFormatter.FormatColumn(column.Entries, authorName, single ? SingleColumnMaxLength : MultiColumnMaxLength);

            components.Add(new TextDisplayProperties($"**{column.Status.ToFormattedString()}**\n{block}"));
        }

        if (refreshable)
        {
            components.Add(new ActionRowProperties([
                new ButtonProperties($"{InteractionIds.BoardRefresh}:{scope}:{BoardHasher.Hash(columns)}", "Refresh", EmojiProperties.Standard("🔄"), ButtonStyle.Secondary),
            ]));
        }

        return MessageFactory.Container(components);
    }
}
