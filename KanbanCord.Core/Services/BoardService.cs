using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;

namespace KanbanCord.Core.Services;

public class BoardService : IBoardService
{
    private readonly ITaskItemRepository _repository;

    public BoardService(ITaskItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BoardColumn>> GetColumnsAsync(ulong guildId, IReadOnlyCollection<BoardStatus> statuses, ulong? assigneeId = null)
    {
        var tasks = await _repository.GetAllTaskItemsByGuildIdAsync(guildId);

        return statuses
            .Select(status => new BoardColumn(
                status,
                tasks
                    .Where(task => task.Status == status)
                    // Number before filtering so the numbers match the unfiltered board
                    .Select((task, index) => new BoardEntry(index + 1, task))
                    .Where(entry => assigneeId is null || entry.Task.AssigneeId == assigneeId)
                    .ToList()))
            .ToList();
    }
}
