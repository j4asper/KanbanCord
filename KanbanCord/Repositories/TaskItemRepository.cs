using KanbanCord.Helpers;
using KanbanCord.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanCord.Repositories;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly IMongoCollection<TaskItem> collection;

    public TaskItemRepository(IMongoDatabase mongoDatabase)
    {
        collection = mongoDatabase.GetCollection<TaskItem>(RequiredCollections.Tasks.ToString());
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId)
    {
        var tasks = await collection.Find(task => task.GuildId == guildId).ToListAsync() ?? [];

        return tasks;
    }

    public async Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId)
    {
        var task = await collection.Find(task => task.Id == objectId).FirstOrDefaultAsync();

        return task;
    }

    public async Task AddTaskItemAsync(TaskItem task)
    {
        await collection.InsertOneAsync(task);
    }

    public async Task UpdateTaskItemAsync(TaskItem task)
    {
        await collection.ReplaceOneAsync(x => x.Id == task.Id, task);
    }

    public async Task RemoveTaskItemAsync(TaskItem task)
    {
        await collection.DeleteOneAsync(x => x.Id == task.Id);
    }

    public async Task RemoveAllTaskItemsByIdAsync(ulong guildId)
    {
        await collection.DeleteManyAsync(x => x.GuildId == guildId);
    }
}