using MongoDB.Bson.Serialization.Attributes;

namespace KanbanCord.Core.Models;

public class Settings
{
    [BsonId]
    public required ulong GuildId { get; set; }
    
    public TimeSpan? ArchiveAfter { get; set; } // Archive completed items after X days
}