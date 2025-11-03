using KanbanCord.Core.Models;
using MongoDB.Bson;

namespace KanbanCord.Core.Services;

public interface ITaskItemService
{
    Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId);
    
    Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId);
    
    Task<ObjectId> CreateOrUpdateTaskItemAsync(TaskItem task);
    
    Task RemoveTaskItemAsync(TaskItem task);
    
    Task RemoveAllTaskItemsByIdAsync(ulong guildId);
}