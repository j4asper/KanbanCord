using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

/// <summary>A task with its 1-based position inside its column.</summary>
public sealed record BoardEntry(int Number, TaskItem Task);

public sealed record BoardColumn(BoardStatus Status, IReadOnlyList<BoardEntry> Entries);
