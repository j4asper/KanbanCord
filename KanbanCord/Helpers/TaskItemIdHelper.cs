using KanbanCord.Models;

namespace KanbanCord.Helpers;

public static class TaskItemIdHelper
{
    public static TaskItem? GetTaskItemByIdOrDefault(this IReadOnlyList<TaskItem> tasks, BoardStatus boardStatus, int taskId)
    {
        var columnTasks = tasks.Where(x => x.Status == boardStatus).ToList();
        
        return columnTasks[taskId - 1];
    }
}