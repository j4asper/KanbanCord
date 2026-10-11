using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public sealed record StatusChange(TaskItem Task, BoardStatus From);

public sealed record PriorityChange(TaskItem Task, Priority? From);

public sealed record AssignmentChange(TaskItem Task, ulong? PreviousAssigneeId);
