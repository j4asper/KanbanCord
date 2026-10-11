using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;

namespace KanbanCord.Tests.PresentationTests;

public class BoardHasherTests
{
    private static IReadOnlyList<BoardColumn> Board(string title, BoardStatus status = BoardStatus.Backlog, ulong? assignee = null) =>
        [new BoardColumn(status, [new BoardEntry(1, new TaskItem { GuildId = 1, Title = title, Description = "d", AuthorId = 1, AssigneeId = assignee, LastUpdatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) })])];

    [Fact]
    public void Hash_ShouldBeStable_ForIdenticalBoards()
    {
        Assert.Equal(BoardHasher.Hash(Board("a")), BoardHasher.Hash(Board("a")));
    }

    [Fact]
    public void Hash_ShouldChange_WhenTitleOrColumnOrAssigneeChanges()
    {
        var baseline = BoardHasher.Hash(Board("a"));

        Assert.NotEqual(baseline, BoardHasher.Hash(Board("b")));
        Assert.NotEqual(baseline, BoardHasher.Hash(Board("a", BoardStatus.Completed)));
        Assert.NotEqual(baseline, BoardHasher.Hash(Board("a", assignee: 5)));
    }

    [Fact]
    public void Hash_ShouldBeEightHexCharacters()
    {
        Assert.Matches("^[0-9a-f]{8}$", BoardHasher.Hash(Board("a")));
    }
}
