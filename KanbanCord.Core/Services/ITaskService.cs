using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public interface ITaskService
{
    Task<TaskItem> GetAsync(ulong guildId, string taskId);

    Task<TaskItem> AddAsync(ulong guildId, ulong authorId, string title, string description, Priority priority = Priority.Medium);

    /// <summary>A null priority leaves the task's current priority unchanged.</summary>
    Task<TaskItem> EditAsync(ulong guildId, string taskId, string title, string description, Priority? priority = null);

    Task<TaskItem> DeleteAsync(ulong guildId, string taskId);

    /// <summary>Throws when the task is already in the requested column.</summary>
    Task<StatusChange> MoveAsync(ulong guildId, string taskId, BoardStatus to);

    Task<StatusChange> StartAsync(ulong guildId, string taskId);

    Task<StatusChange> CompleteAsync(ulong guildId, string taskId);

    Task<StatusChange> ArchiveAsync(ulong guildId, string taskId);

    /// <summary>Throws when the task already has the requested priority.</summary>
    Task<PriorityChange> SetPriorityAsync(ulong guildId, string taskId, Priority priority);

    /// <summary>A null assignee removes the current one; throws when nobody is assigned.</summary>
    Task<AssignmentChange> AssignAsync(ulong guildId, string taskId, ulong? assigneeId);

    Task<TaskItem> AddCommentAsync(ulong guildId, string taskId, ulong authorId, string text);

    /// <summary>Archives every non-archived task of the guild and returns how many were archived.</summary>
    Task<int> ClearBoardAsync(ulong guildId);

    /// <summary>Deletes every task of the guild.</summary>
    Task ResetBoardAsync(ulong guildId);
}
