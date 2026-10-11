using KanbanCord.Core.Exceptions;
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using KanbanCord.Core.Services;
using KanbanCord.Tests.Factories;
using Mongo2Go;
using MongoDB.Driver;

namespace KanbanCord.Tests.ServiceTests;

public class TaskServiceMongoTests : IDisposable
{
    private const ulong GuildId = TaskItemFactory.DefaultGuildId;

    private readonly MongoDbRunner _runner;
    private readonly TaskService _service;
    private readonly BoardService _boardService;

    public TaskServiceMongoTests()
    {
        _runner = MongoDbRunner.Start();

        var database = new MongoClient(_runner.ConnectionString).GetDatabase("KanbanCord");
        var repository = new TaskItemRepository(database);

        _service = new TaskService(repository, TimeProvider.System);
        _boardService = new BoardService(repository);
    }

    [Fact]
    public async Task TaskLifecycle_ShouldWorkAgainstRealMongo()
    {
        // Arrange & Act
        var added = await _service.AddAsync(GuildId, 1, "Ship it", "Release v2");
        var id = added.Id.ToString();

        await _service.EditAsync(GuildId, id, "Ship it now", "Release v2.0");
        await _service.AddCommentAsync(GuildId, id, 2, "Looks good to me!");
        await _service.StartAsync(GuildId, id);
        await _service.AssignAsync(GuildId, id, 3);
        await _service.SetPriorityAsync(GuildId, id, Priority.High);

        // Assert
        var stored = await _service.GetAsync(GuildId, id);
        Assert.Equal("Ship it now", stored.Title);
        Assert.Equal(BoardStatus.InProgress, stored.Status);
        Assert.Equal(3UL, stored.AssigneeId);
        Assert.Equal(Priority.High, stored.Priority);
        Assert.Equal("Looks good to me!", Assert.Single(stored.Comments).Text);

        var board = await _boardService.GetColumnsAsync(GuildId, [BoardStatus.Backlog, BoardStatus.InProgress]);
        Assert.Empty(board[0].Entries);
        Assert.Equal(1, Assert.Single(board[1].Entries).Number);
    }

    [Fact]
    public async Task ClearThenReset_ShouldArchiveThenDeleteOnlyTheGuildsTasks()
    {
        // Arrange
        await _service.AddAsync(GuildId, 1, "One", "d");
        await _service.AddAsync(GuildId, 1, "Two", "d");
        await _service.AddAsync(1, 1, "Foreign", "d");

        // Act & Assert
        Assert.Equal(2, await _service.ClearBoardAsync(GuildId));
        var archived = await _boardService.GetColumnsAsync(GuildId, [BoardStatus.Archived]);
        Assert.Equal(2, archived[0].Entries.Count);

        await _service.ResetBoardAsync(GuildId);
        Assert.Empty((await _boardService.GetColumnsAsync(GuildId, [BoardStatus.Archived]))[0].Entries);
        Assert.Single((await _boardService.GetColumnsAsync(1, [BoardStatus.Backlog]))[0].Entries);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowNotFound_ForTaskOfAnotherGuild()
    {
        // Arrange
        var foreign = await _service.AddAsync(1, 1, "Foreign", "d");

        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, foreign.Id.ToString()));
    }

    public void Dispose() => _runner.Dispose();
}
