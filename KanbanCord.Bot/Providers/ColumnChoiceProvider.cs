using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Providers;

public class ColumnChoiceProvider : IChoiceProvider
{
    private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Columns =
    [
        new(BoardStatus.Backlog.ToFormattedString(), (int)BoardStatus.Backlog),
        new(BoardStatus.InProgress.ToFormattedString(), (int)BoardStatus.InProgress),
        new(BoardStatus.Completed.ToFormattedString(), (int)BoardStatus.Completed),
        new(BoardStatus.Archived.ToFormattedString(), (int)BoardStatus.Archived)
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) =>
        ValueTask.FromResult(Columns.AsEnumerable());
}