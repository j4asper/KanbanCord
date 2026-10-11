namespace KanbanCord.Core.Models;

public class Comment
{
    public required ulong AuthorId { get; set; }
    
    public required string Text { get; set; }
    
    public required DateTime CreatedAt { get; set; }
}