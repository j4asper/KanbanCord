using KanbanCord.Core.Models;
using MongoDB.Bson;

namespace KanbanCord.Core.Repositories;

public interface ITaskItemRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId);
    
    Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId);
    
    Task CreateOrUpdateTaskItemAsync(TaskItem task);
    
    Task RemoveTaskItemAsync(TaskItem task);
    
    Task RemoveAllTaskItemsByIdAsync(ulong guildId);
}