using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using KanbanCord.Core.Services;
using MongoDB.Bson;

namespace KanbanCord.DiscordApplication.Services;

public class TaskItemService : ITaskItemService
{
    private readonly ITaskItemRepository _taskItemRepository;

    public TaskItemService(ITaskItemRepository taskItemRepository)
    {
        _taskItemRepository = taskItemRepository;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId)
    {
        var tasks = await _taskItemRepository.GetAllTaskItemsByGuildIdAsync(guildId);
        
        return tasks;
    }

    public async Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId)
    {
        var task = await _taskItemRepository.GetTaskItemByObjectIdOrDefaultAsync(objectId);
        
        return task;
    }

    public async Task CreateOrUpdateTaskItemAsync(TaskItem task)
    {
        await _taskItemRepository.CreateOrUpdateTaskItemAsync(task);
    }

    public async Task RemoveTaskItemAsync(TaskItem task)
    {
        await _taskItemRepository.RemoveTaskItemAsync(task);
    }

    public async Task RemoveAllTaskItemsByIdAsync(ulong guildId)
    {
        await _taskItemRepository.RemoveAllTaskItemsByIdAsync(guildId);
    }
}