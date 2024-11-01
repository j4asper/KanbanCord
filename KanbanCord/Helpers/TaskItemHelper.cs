using DSharpPlus;
using KanbanCord.Models;

namespace KanbanCord.Helpers;

public static class TaskItemHelper
{
    public static TaskItem? GetTaskItemByIdOrDefault(this IReadOnlyList<TaskItem> tasks, BoardStatus boardStatus, int taskId)
    {
        var columnTasks = tasks.Where(x => x.Status == boardStatus).ToList();

        if (columnTasks.Count == 0 || taskId - 1 > columnTasks.Count)
            return null;
        
        return columnTasks[taskId - 1];
    }
    
    public static async Task<string> GetBoardTaskString(this IReadOnlyList<TaskItem> boardItems, DiscordClient client, BoardStatus boardStatus)
    {
        List<string> taskStrings = [];
        
        var id = 1;
        
        foreach (var boardItem in boardItems.Where(x => x.Status == boardStatus))
        {
            var user = await client.GetUserAsync(boardItem.AuthorId);
            
            taskStrings.Add($"{id} - \"{boardItem.Title}\" added by: {user.Username}");
            
            id++;
        }
        
        return $"```bash\n{(taskStrings.Any() ? string.Join('\n', taskStrings) : " ")}```";
    }
}