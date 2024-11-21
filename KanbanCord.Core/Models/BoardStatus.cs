using System.Text.RegularExpressions;

namespace KanbanCord.Core.Models;

public enum BoardStatus
{
    Backlog,
    InProgress,
    Completed,
    Archived
}

public static class EnumExtensions
{
    public static string ToFormattedString(this BoardStatus status)
    {
        return Regex.Replace(status.ToString(), "([A-Z])", " $1").Trim();
    }
}