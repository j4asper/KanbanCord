using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Providers;

public class PriorityChoiceProvider : IChoiceProvider
{
    private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Priorities =
    [
        new(Priority.Low.ToString(), (int)Priority.Low),
        new(Priority.Medium.ToString(), (int)Priority.Medium),
        new(Priority.High.ToString(), (int)Priority.High)
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) =>
        ValueTask.FromResult(Priorities.AsEnumerable());
}