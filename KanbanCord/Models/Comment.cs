namespace KanbanCord.Models;

public class Comment
{
    public required ulong AuthorId { get; set; }
    
    public required string Text { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}