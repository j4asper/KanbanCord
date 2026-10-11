using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.Tests.Factories;
using KanbanCord.Tests.Fakes;

namespace KanbanCord.Tests.ServiceTests;

public class BoardServiceTests
{
    private const ulong GuildId = 1;

    private readonly InMemoryTaskItemRepository _repository = new();
    private readonly BoardService _service;

    public BoardServiceTests()
    {
        _service = new BoardService(_repository);
    }

    private async Task SeedAsync(string title, BoardStatus status, ulong guildId = GuildId, ulong? assigneeId = null) =>
        await _repository.AddTaskItemAsync(TaskItemFactory.Create(guildId, title, status, assigneeId: assigneeId));

    [Fact]
    public async Task GetColumnsAsync_ShouldReturnOneColumnPerRequestedStatusInOrder()
    {
        // Arrange
        await SeedAsync("a", BoardStatus.Completed);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed]);

        // Assert
        Assert.Equal([BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed], columns.Select(c => c.Status));
        Assert.Empty(columns[0].Entries);
        Assert.Single(columns[2].Entries);
    }

    [Fact]
    public async Task GetColumnsAsync_ShouldNumberEntriesPerColumnStartingAtOne()
    {
        // Arrange
        await SeedAsync("b1", BoardStatus.Backlog);
        await SeedAsync("p1", BoardStatus.InProgress);
        await SeedAsync("b2", BoardStatus.Backlog);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog, BoardStatus.InProgress]);

        // Assert
        Assert.Equal([(1, "b1"), (2, "b2")], columns[0].Entries.Select(e => (e.Number, e.Task.Title)));
        Assert.Equal([(1, "p1")], columns[1].Entries.Select(e => (e.Number, e.Task.Title)));
    }

    [Fact]
    public async Task GetColumnsAsync_ShouldKeepOriginalNumbersWhenFilteringByAssignee()
    {
        // Arrange
        await SeedAsync("unassigned", BoardStatus.Backlog);
        await SeedAsync("mine", BoardStatus.Backlog, assigneeId: 5);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog], assigneeId: 5);

        // Assert
        var entry = Assert.Single(columns[0].Entries);
        Assert.Equal(2, entry.Number);
        Assert.Equal("mine", entry.Task.Title);
    }

    [Fact]
    public async Task GetColumnsAsync_ShouldIgnoreOtherGuildsAndNotIncludeUnrequestedStatuses()
    {
        // Arrange
        await SeedAsync("mine", BoardStatus.Backlog);
        await SeedAsync("foreign", BoardStatus.Backlog, guildId: 2);
        await SeedAsync("old", BoardStatus.Archived);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog]);

        // Assert
        Assert.Equal("mine", Assert.Single(Assert.Single(columns).Entries).Task.Title);
    }
}
