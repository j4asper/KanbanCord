using KanbanCord.Core.Models;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public enum PriorityChoice
{
    [SlashCommandChoice(Name = "Low")] Low,
    [SlashCommandChoice(Name = "Medium")] Medium,
    [SlashCommandChoice(Name = "High")] High,
}

public static class PriorityChoiceExtensions
{
    public static Priority ToPriority(this PriorityChoice choice) => choice switch
    {
        PriorityChoice.Low => Priority.Low,
        PriorityChoice.High => Priority.High,
        _ => Priority.Medium,
    };
}
