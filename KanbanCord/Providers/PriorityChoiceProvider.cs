using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using KanbanCord.Models;

namespace KanbanCord.Providers;

public class PriorityChoiceProvider : IChoiceProvider
{
    private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> priorities =
    [
        new(Priority.Low.ToString(), (int)Priority.Low),
        new(Priority.Medium.ToString(), (int)Priority.Medium),
        new(Priority.High.ToString(), (int)Priority.High)
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) =>
        ValueTask.FromResult(priorities.AsEnumerable());
}