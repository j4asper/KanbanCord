using KanbanCord.Models;

namespace KanbanCord.Helpers;

public static class TaskItemIdHelper
{
    public static TaskItem? GetTaskItemByIdOrDefault(this IReadOnlyList<TaskItem> tasks, BoardStatus boardStatus, int taskId)
    {
        var columnTasks = tasks.Where(x => x.Status == boardStatus).ToList();

        if (columnTasks.Count == 0 || taskId - 1 > columnTasks.Count)
            return null;
        
        return columnTasks[taskId - 1];
    }
}