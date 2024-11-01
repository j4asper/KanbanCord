using MongoDB.Bson.Serialization.Attributes;

namespace KanbanCord.Models;

public class Settings
{
    [BsonId]
    public required ulong GuildId { get; set; }
    
    public TimeSpan? ArchiveAfter { get; set; } // Archive completed items after X days
}