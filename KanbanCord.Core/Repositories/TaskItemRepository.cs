using KanbanCord.Core.Interfaces;
using KanbanCord.Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanCord.Core.Repositories;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly IMongoCollection<TaskItem> _collection;

    public TaskItemRepository(IMongoDatabase mongoDatabase)
    {
        _collection = mongoDatabase.GetCollection<TaskItem>(nameof(RequiredCollections.Tasks));
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId)
    {
        var tasks = await _collection.Find(task => task.GuildId == guildId).ToListAsync() ?? [];

        return tasks;
    }

    public async Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId)
    {
        var task = await _collection.Find(task => task.Id == objectId).FirstOrDefaultAsync();

        return task;
    }

    public async Task AddTaskItemAsync(TaskItem task)
    {
        await _collection.InsertOneAsync(task);
    }

    public async Task UpdateTaskItemAsync(TaskItem task)
    {
        await _collection.ReplaceOneAsync(x => x.Id == task.Id, task);
    }

    public async Task RemoveTaskItemAsync(TaskItem task)
    {
        await _collection.DeleteOneAsync(x => x.Id == task.Id);
    }

    public async Task RemoveAllTaskItemsByIdAsync(ulong guildId)
    {
        await _collection.DeleteManyAsync(x => x.GuildId == guildId);
    }
}