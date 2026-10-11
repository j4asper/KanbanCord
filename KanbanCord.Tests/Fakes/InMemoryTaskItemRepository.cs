using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using MongoDB.Bson;

namespace KanbanCord.Tests.Fakes;

public class InMemoryTaskItemRepository : ITaskItemRepository
{
    public List<TaskItem> Items { get; } = [];

    public Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(Items.Where(x => x.GuildId == guildId).Select(Clone).ToList());

    public Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId)
    {
        var item = Items.FirstOrDefault(x => x.Id == objectId);

        return Task.FromResult(item is null ? null : Clone(item));
    }

    public Task AddTaskItemAsync(TaskItem task)
    {
        if (task.Id == ObjectId.Empty)
            task.Id = ObjectId.GenerateNewId();

        Items.Add(Clone(task));

        return Task.CompletedTask;
    }

    public Task UpdateTaskItemAsync(TaskItem task)
    {
        var index = Items.FindIndex(x => x.Id == task.Id);

        if (index >= 0)
            Items[index] = Clone(task);

        return Task.CompletedTask;
    }

    public Task RemoveTaskItemAsync(TaskItem task)
    {
        Items.RemoveAll(x => x.Id == task.Id);

        return Task.CompletedTask;
    }

    public Task RemoveAllTaskItemsByIdAsync(ulong guildId)
    {
        Items.RemoveAll(x => x.GuildId == guildId);

        return Task.CompletedTask;
    }

    private static TaskItem Clone(TaskItem source) => new()
    {
        Id = source.Id,
        GuildId = source.GuildId,
        Title = source.Title,
        Description = source.Description,
        CreatedAt = source.CreatedAt,
        LastUpdatedAt = source.LastUpdatedAt,
        AuthorId = source.AuthorId,
        AssigneeId = source.AssigneeId,
        Comments = source.Comments.Select(c => new Comment { AuthorId = c.AuthorId, Text = c.Text, CreatedAt = c.CreatedAt }).ToList(),
        Status = source.Status,
        Priority = source.Priority,
    };
}
