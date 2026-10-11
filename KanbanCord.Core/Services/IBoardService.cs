using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public interface IBoardService
{
    Task<IReadOnlyList<BoardColumn>> GetColumnsAsync(ulong guildId, IReadOnlyCollection<BoardStatus> statuses, ulong? assigneeId = null);
}
