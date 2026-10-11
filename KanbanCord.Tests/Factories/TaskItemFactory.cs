using KanbanCord.Core.Models;

namespace KanbanCord.Tests.Factories;

public static class TaskItemFactory
{
    public const ulong DefaultGuildId = 123456789012345678;

    public static TaskItem Create(
        ulong guildId = DefaultGuildId,
        string title = "Test Task",
        BoardStatus status = BoardStatus.Backlog,
        ulong authorId = 987654321098765432,
        ulong? assigneeId = null) =>
        new()
        {
            GuildId = guildId,
            Title = title,
            Description = "Test Task Description",
            AuthorId = authorId,
            AssigneeId = assigneeId,
            Status = status,
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LastUpdatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

    public static IReadOnlyList<TaskItem> CreateMany() =>
    [
        Create(title: "Backlog Task", status: BoardStatus.Backlog),
        Create(title: "In Progress Task", status: BoardStatus.InProgress),
        Create(title: "Completed Task", status: BoardStatus.Completed),
        Create(title: "Archived Task", status: BoardStatus.Archived),
        Create(guildId: 1, title: "Other Guild Task"),
    ];
}
