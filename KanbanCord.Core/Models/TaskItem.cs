using MongoDB.Bson;

namespace KanbanCord.Core.Models;

public class TaskItem
{
    public ObjectId Id { get; set; }
    
    public required ulong GuildId { get; set; }
    
    public required string Title { get; set; }
    
    public required string Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    public required ulong AuthorId { get; set; }
    
    public ulong? AssigneeId { get; set; }

    public List<Comment> Comments { get; set; } = [];
    
    public BoardStatus Status { get; set; } = BoardStatus.Backlog;
    
    public Priority Priority { get; set; } = Models.Priority.Medium;
}