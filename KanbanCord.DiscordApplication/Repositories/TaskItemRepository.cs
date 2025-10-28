using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanCord.DiscordApplication.Repositories;

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

    public async Task CreateOrUpdateTaskItemAsync(TaskItem task)
    {
        var filter = Builders<TaskItem>.Filter.Eq(x => x.Id, task.Id);

        await _collection.ReplaceOneAsync(
            filter,
            task,
            new ReplaceOptions { IsUpsert = true }
        );
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