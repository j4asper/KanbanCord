using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace KanbanCord.Providers;

public class ColumnChoiceProvider : IChoiceProvider
{
    private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> columns =
    [
        new("Backlog", 0),
        new("In Progress", 1),
        new("Completed", 2),
        new("Archived", 3)
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) =>
        ValueTask.FromResult(columns.AsEnumerable());
}