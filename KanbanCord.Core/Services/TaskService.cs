using KanbanCord.Core.Constants;
using KanbanCord.Core.Exceptions;
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using MongoDB.Bson;

namespace KanbanCord.Core.Services;

public class TaskService : ITaskService
{
    private const string TaskNotFoundMessage = "The selected task was not found, please try again.";

    private readonly ITaskItemRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TaskService(ITaskItemRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<TaskItem> GetAsync(ulong guildId, string taskId)
    {
        if (!ObjectId.TryParse(taskId, out var objectId))
            throw new KanbanCordException(TaskNotFoundMessage);

        var task = await _repository.GetTaskItemByObjectIdOrDefaultAsync(objectId);

        // A task from another guild is indistinguishable from a missing one
        if (task is null || task.GuildId != guildId)
            throw new KanbanCordException(TaskNotFoundMessage);

        return task;
    }

    public async Task<TaskItem> AddAsync(ulong guildId, ulong authorId, string title, string description, Priority priority = Priority.Medium)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var task = new TaskItem
        {
            GuildId = guildId,
            AuthorId = authorId,
            Title = RequireText(title, Limits.TaskTitleMaxLength, "title"),
            Description = RequireText(description, Limits.TaskDescriptionMaxLength, "description"),
            Priority = priority,
            CreatedAt = now,
            LastUpdatedAt = now,
        };

        await _repository.AddTaskItemAsync(task);

        return task;
    }

    public async Task<TaskItem> EditAsync(ulong guildId, string taskId, string title, string description, Priority? priority = null)
    {
        var task = await GetAsync(guildId, taskId);

        task.Title = RequireText(title, Limits.TaskTitleMaxLength, "title");
        task.Description = RequireText(description, Limits.TaskDescriptionMaxLength, "description");

        if (priority is not null)
            task.Priority = priority;

        await SaveAsync(task);

        return task;
    }

    public async Task<TaskItem> DeleteAsync(ulong guildId, string taskId)
    {
        var task = await GetAsync(guildId, taskId);

        await _repository.RemoveTaskItemAsync(task);

        return task;
    }

    public async Task<StatusChange> MoveAsync(ulong guildId, string taskId, BoardStatus to)
    {
        var task = await GetAsync(guildId, taskId);

        if (task.Status == to)
            throw new KanbanCordException($"The selected task is already in column **{to.ToFormattedString()}**.");

        return await ChangeStatusAsync(task, to);
    }

    public async Task<StatusChange> StartAsync(ulong guildId, string taskId) =>
        await ChangeStatusAsync(await GetAsync(guildId, taskId), BoardStatus.InProgress);

    public async Task<StatusChange> CompleteAsync(ulong guildId, string taskId) =>
        await ChangeStatusAsync(await GetAsync(guildId, taskId), BoardStatus.Completed);

    public async Task<StatusChange> ArchiveAsync(ulong guildId, string taskId) =>
        await ChangeStatusAsync(await GetAsync(guildId, taskId), BoardStatus.Archived);

    public async Task<PriorityChange> SetPriorityAsync(ulong guildId, string taskId, Priority priority)
    {
        var task = await GetAsync(guildId, taskId);

        if (task.Priority == priority)
            throw new KanbanCordException($"The selected task already has the priority **{priority}**.");

        var from = task.Priority;

        task.Priority = priority;

        await SaveAsync(task);

        return new PriorityChange(task, from);
    }

    public async Task<AssignmentChange> AssignAsync(ulong guildId, string taskId, ulong? assigneeId)
    {
        var task = await GetAsync(guildId, taskId);

        if (assigneeId is null && task.AssigneeId is null)
            throw new KanbanCordException("The selected task has no user assigned.");

        var previous = task.AssigneeId;

        task.AssigneeId = assigneeId;

        await SaveAsync(task);

        return new AssignmentChange(task, previous);
    }

    public async Task<TaskItem> AddCommentAsync(ulong guildId, string taskId, ulong authorId, string text)
    {
        var task = await GetAsync(guildId, taskId);

        task.Comments.Add(new Comment
        {
            AuthorId = authorId,
            Text = RequireText(text, Limits.TaskCommentMaxLength, "comment", Limits.TaskCommentMinLength),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        });

        await SaveAsync(task);

        return task;
    }

    public async Task<int> ClearBoardAsync(ulong guildId)
    {
        var tasks = await _repository.GetAllTaskItemsByGuildIdAsync(guildId);

        var archived = 0;

        foreach (var task in tasks.Where(x => x.Status != BoardStatus.Archived))
        {
            task.Status = BoardStatus.Archived;

            await SaveAsync(task);

            archived++;
        }

        return archived;
    }

    public async Task ResetBoardAsync(ulong guildId)
    {
        await _repository.RemoveAllTaskItemsByIdAsync(guildId);
    }

    private async Task<StatusChange> ChangeStatusAsync(TaskItem task, BoardStatus to)
    {
        var from = task.Status;

        task.Status = to;

        await SaveAsync(task);

        return new StatusChange(task, from);
    }

    private async Task SaveAsync(TaskItem task)
    {
        task.LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _repository.UpdateTaskItemAsync(task);
    }

    private static string RequireText(string? value, int maxLength, string fieldName, int minLength = 1)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            throw new KanbanCordException($"The {fieldName} cannot be empty.");

        if (trimmed.Length < minLength)
            throw new KanbanCordException($"The {fieldName} must be at least {minLength} characters long.");

        if (trimmed.Length > maxLength)
            throw new KanbanCordException($"The {fieldName} cannot be longer than {maxLength} characters.");

        return trimmed;
    }
}
