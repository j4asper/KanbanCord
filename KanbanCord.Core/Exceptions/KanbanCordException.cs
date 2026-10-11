namespace KanbanCord.Core.Exceptions;

/// <summary>A failure whose message is safe and intended to be shown to the Discord user.</summary>
public class KanbanCordException : Exception
{
    public KanbanCordException(string message) : base(message)
    {
    }
}
