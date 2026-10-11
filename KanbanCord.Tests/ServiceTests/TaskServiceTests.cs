using KanbanCord.Core.Constants;
using KanbanCord.Core.Exceptions;
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.Tests.Factories;
using KanbanCord.Tests.Fakes;

namespace KanbanCord.Tests.ServiceTests;

public class TaskServiceTests
{
    private const ulong GuildId = 1;
    private const ulong OtherGuildId = 2;
    private const string NotFound = "The selected task was not found, please try again.";

    private readonly InMemoryTaskItemRepository _repository = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repository, _time);
    }

    private async Task<TaskItem> SeedAsync(ulong guildId = GuildId, BoardStatus status = BoardStatus.Backlog, string title = "Task")
    {
        var task = TaskItemFactory.Create(guildId, title, status);

        await _repository.AddTaskItemAsync(task);

        return task;
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTask_WhenTaskBelongsToGuild()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        var result = await _service.GetAsync(GuildId, seeded.Id.ToString());

        // Assert
        Assert.Equal(seeded.Id, result.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldThrow_WhenTaskBelongsToOtherGuild()
    {
        // Arrange
        var seeded = await SeedAsync(OtherGuildId);

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, seeded.Id.ToString()));

        // Assert
        Assert.Equal(NotFound, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    public async Task GetAsync_ShouldThrowNotFound_WhenIdIsMalformed(string taskId)
    {
        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, taskId));

        // Assert
        Assert.Equal(NotFound, exception.Message);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, "507f1f77bcf86cd799439011"));

        // Assert
        Assert.Equal(NotFound, exception.Message);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTrimmedTaskInBacklog()
    {
        // Act
        var result = await _service.AddAsync(GuildId, 42, "  Fix bug  ", " Details ");

        // Assert
        var stored = Assert.Single(_repository.Items);
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal("Fix bug", stored.Title);
        Assert.Equal("Details", stored.Description);
        Assert.Equal(GuildId, stored.GuildId);
        Assert.Equal(42UL, stored.AuthorId);
        Assert.Equal(BoardStatus.Backlog, stored.Status);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    public async Task AddAsync_ShouldPersistTheChosenPriority(Priority priority)
    {
        // Act
        await _service.AddAsync(GuildId, 1, "Fix bug", "Details", priority);

        // Assert
        Assert.Equal(priority, Assert.Single(_repository.Items).Priority);
    }

    [Fact]
    public async Task AddAsync_ShouldDefaultToMediumPriority_WhenNoneIsGiven()
    {
        // Act
        await _service.AddAsync(GuildId, 1, "Fix bug", "Details");

        // Assert
        Assert.Equal(Priority.Medium, Assert.Single(_repository.Items).Priority);
    }

    [Theory]
    [InlineData("", "desc")]
    [InlineData("   ", "desc")]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    public async Task AddAsync_ShouldThrow_WhenTitleOrDescriptionIsBlank(string title, string description)
    {
        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddAsync(GuildId, 1, title, description));
        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenTitleOrDescriptionIsTooLong()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddAsync(GuildId, 1, new string('a', Limits.TaskTitleMaxLength + 1), "desc"));
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddAsync(GuildId, 1, "title", new string('a', Limits.TaskDescriptionMaxLength + 1)));
        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateTitleDescriptionAndLastUpdatedAt()
    {
        // Arrange
        var seeded = await SeedAsync();
        _time.Advance(TimeSpan.FromHours(1));

        // Act
        await _service.EditAsync(GuildId, seeded.Id.ToString(), "New title", "New description");

        // Assert
        var stored = Assert.Single(_repository.Items);
        Assert.Equal("New title", stored.Title);
        Assert.Equal("New description", stored.Description);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, stored.LastUpdatedAt);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdatePriority_WhenProvided()
    {
        // Arrange
        var seeded = await SeedAsync(); // Medium by default

        // Act
        await _service.EditAsync(GuildId, seeded.Id.ToString(), "Title", "Description", Priority.High);

        // Assert
        Assert.Equal(Priority.High, Assert.Single(_repository.Items).Priority);
    }

    [Fact]
    public async Task EditAsync_ShouldKeepPriority_WhenNotProvided()
    {
        // Arrange
        var seeded = await SeedAsync();
        await _service.SetPriorityAsync(GuildId, seeded.Id.ToString(), Priority.Low);

        // Act
        await _service.EditAsync(GuildId, seeded.Id.ToString(), "Title", "Description");

        // Assert
        Assert.Equal(Priority.Low, Assert.Single(_repository.Items).Priority);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTaskAndReturnIt()
    {
        // Arrange
        var seeded = await SeedAsync(title: "Doomed");

        // Act
        var deleted = await _service.DeleteAsync(GuildId, seeded.Id.ToString());

        // Assert
        Assert.Equal("Doomed", deleted.Title);
        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task MoveAsync_ShouldChangeStatusAndReturnPreviousColumn()
    {
        // Arrange
        var seeded = await SeedAsync(status: BoardStatus.Backlog);

        // Act
        var change = await _service.MoveAsync(GuildId, seeded.Id.ToString(), BoardStatus.Completed);

        // Assert
        Assert.Equal(BoardStatus.Backlog, change.From);
        Assert.Equal(BoardStatus.Completed, Assert.Single(_repository.Items).Status);
    }

    [Fact]
    public async Task MoveAsync_ShouldThrow_WhenTaskAlreadyInColumn()
    {
        // Arrange
        var seeded = await SeedAsync(status: BoardStatus.InProgress);

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.MoveAsync(GuildId, seeded.Id.ToString(), BoardStatus.InProgress));

        // Assert
        Assert.Equal("The selected task is already in column **In Progress**.", exception.Message);
    }

    [Fact]
    public async Task StartCompleteArchive_ShouldSetTargetStatuses()
    {
        // Arrange
        var seeded = await SeedAsync();
        var id = seeded.Id.ToString();

        // Act & Assert
        Assert.Equal(BoardStatus.Backlog, (await _service.StartAsync(GuildId, id)).From);
        Assert.Equal(BoardStatus.InProgress, Assert.Single(_repository.Items).Status);

        Assert.Equal(BoardStatus.InProgress, (await _service.CompleteAsync(GuildId, id)).From);
        Assert.Equal(BoardStatus.Completed, Assert.Single(_repository.Items).Status);

        Assert.Equal(BoardStatus.Completed, (await _service.ArchiveAsync(GuildId, id)).From);
        Assert.Equal(BoardStatus.Archived, Assert.Single(_repository.Items).Status);
    }

    [Fact]
    public async Task SetPriorityAsync_ShouldUpdateAndReturnPreviousPriority()
    {
        // Arrange
        var seeded = await SeedAsync(); // default priority is Medium

        // Act
        var change = await _service.SetPriorityAsync(GuildId, seeded.Id.ToString(), Priority.High);

        // Assert
        Assert.Equal(Priority.Medium, change.From);
        Assert.Equal(Priority.High, Assert.Single(_repository.Items).Priority);
    }

    [Fact]
    public async Task SetPriorityAsync_ShouldThrow_WhenPriorityUnchanged()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.SetPriorityAsync(GuildId, seeded.Id.ToString(), Priority.Medium));

        // Assert
        Assert.Equal("The selected task already has the priority **Medium**.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldAssignThenUnassign()
    {
        // Arrange
        var seeded = await SeedAsync();
        var id = seeded.Id.ToString();

        // Act
        var assigned = await _service.AssignAsync(GuildId, id, 99);
        var unassigned = await _service.AssignAsync(GuildId, id, null);

        // Assert
        Assert.Null(assigned.PreviousAssigneeId);
        Assert.Equal(99UL, unassigned.PreviousAssigneeId);
        Assert.Null(Assert.Single(_repository.Items).AssigneeId);
    }

    [Fact]
    public async Task AssignAsync_ShouldThrow_WhenUnassigningTaskWithoutAssignee()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.AssignAsync(GuildId, seeded.Id.ToString(), null));

        // Assert
        Assert.Equal("The selected task has no user assigned.", exception.Message);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldAppendCommentWithAuthorAndTimestamp()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        await _service.AddCommentAsync(GuildId, seeded.Id.ToString(), 7, "This is a valid comment.");

        // Assert
        var comment = Assert.Single(Assert.Single(_repository.Items).Comments);
        Assert.Equal(7UL, comment.AuthorId);
        Assert.Equal("This is a valid comment.", comment.Text);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, comment.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("too short")] // 9 characters, minimum is 10
    public async Task AddCommentAsync_ShouldThrow_WhenCommentBelowMinimumOrBlank(string text)
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddCommentAsync(GuildId, seeded.Id.ToString(), 7, text));
        Assert.Empty(Assert.Single(_repository.Items).Comments);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldThrow_WhenCommentTooLong()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddCommentAsync(GuildId, seeded.Id.ToString(), 7, new string('a', Limits.TaskCommentMaxLength + 1)));
    }

    [Fact]
    public async Task ClearBoardAsync_ShouldArchiveEveryNonArchivedTaskOfTheGuildOnly()
    {
        // Arrange
        await SeedAsync(status: BoardStatus.Backlog);
        await SeedAsync(status: BoardStatus.Completed);
        await SeedAsync(status: BoardStatus.Archived);
        var foreign = await SeedAsync(OtherGuildId, BoardStatus.Backlog);

        // Act
        var archived = await _service.ClearBoardAsync(GuildId);

        // Assert
        Assert.Equal(2, archived);
        Assert.All(_repository.Items.Where(x => x.GuildId == GuildId), x => Assert.Equal(BoardStatus.Archived, x.Status));
        Assert.Equal(BoardStatus.Backlog, _repository.Items.Single(x => x.Id == foreign.Id).Status);
    }

    [Fact]
    public async Task ResetBoardAsync_ShouldDeleteOnlyTheGuildsTasks()
    {
        // Arrange
        await SeedAsync();
        await SeedAsync(status: BoardStatus.Archived);
        await SeedAsync(OtherGuildId);

        // Act
        await _service.ResetBoardAsync(GuildId);

        // Assert
        Assert.Equal(OtherGuildId, Assert.Single(_repository.Items).GuildId);
    }

    public static IEnumerable<object[]> Mutations() =>
    [
        [(Func<ITaskService, string, Task>)((s, id) => s.EditAsync(GuildId, id, "t", "d"))],
        [(Func<ITaskService, string, Task>)((s, id) => s.DeleteAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.MoveAsync(GuildId, id, BoardStatus.Completed))],
        [(Func<ITaskService, string, Task>)((s, id) => s.StartAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.CompleteAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.ArchiveAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.SetPriorityAsync(GuildId, id, Priority.High))],
        [(Func<ITaskService, string, Task>)((s, id) => s.AssignAsync(GuildId, id, 5))],
        [(Func<ITaskService, string, Task>)((s, id) => s.AddCommentAsync(GuildId, id, 5, "A perfectly fine comment"))],
    ];

    [Theory]
    [MemberData(nameof(Mutations))]
    public async Task Mutations_ShouldRejectTaskFromOtherGuild(Func<ITaskService, string, Task> mutation)
    {
        // Arrange
        var foreign = await SeedAsync(OtherGuildId, title: "Foreign");

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => mutation(_service, foreign.Id.ToString()));

        // Assert
        Assert.Equal(NotFound, exception.Message);
        var stored = Assert.Single(_repository.Items);
        Assert.Equal("Foreign", stored.Title);
        Assert.Equal(BoardStatus.Backlog, stored.Status);
        Assert.Empty(stored.Comments);
    }
}
