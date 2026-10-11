using KanbanCord.Core.Models;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public enum ColumnChoice
{
    [SlashCommandChoice(Name = "Backlog")] Backlog,
    [SlashCommandChoice(Name = "In Progress")] InProgress,
    [SlashCommandChoice(Name = "Completed")] Completed,
    [SlashCommandChoice(Name = "Archived")] Archived,
}

public static class ColumnChoiceExtensions
{
    public static BoardStatus ToBoardStatus(this ColumnChoice choice) => choice switch
    {
        ColumnChoice.Backlog => BoardStatus.Backlog,
        ColumnChoice.InProgress => BoardStatus.InProgress,
        ColumnChoice.Completed => BoardStatus.Completed,
        _ => BoardStatus.Archived,
    };
}
