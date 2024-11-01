using KanbanCord.Models;

namespace KanbanCord.Repositories;

public interface ITaskItemRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId);
    
    Task AddTaskItemAsync(TaskItem task);
    
    Task UpdateTaskItemAsync(TaskItem task);
    
    Task RemoveTaskItemAsync(TaskItem task);
    
    Task RemoveAllTaskItemsByIdAsync(ulong guildId);
}