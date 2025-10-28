using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Repositories;
using Mongo2Go;
using MongoDB.Driver;
using MongoDB.Bson;

namespace KanbanCord.Tests.RepositoryTests;

public class TaskItemRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;
    private readonly TaskItemRepository _repository;

    public TaskItemRepositoryTests()
    {
        _runner = MongoDbRunner.Start();

        _database = new MongoClient(_runner.ConnectionString)
            .GetDatabase("KanbanCord");

        _repository = new TaskItemRepository(_database);
    }

    [Fact]
    public async Task GetAllTaskItemsByGuildIdAsync_ShouldReturnAllTasksByGuildId()
    {
        // Arrange
        var task1 = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task 1",
            Description = "Description 1",
            AuthorId = 987654321
        };
        var task2 = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task 2",
            Description = "Description 2",
            AuthorId = 987654322
        };

        await _repository.CreateOrUpdateTaskItemAsync(task1);
        await _repository.CreateOrUpdateTaskItemAsync(task2);

        // Act
        var result = await _repository.GetAllTaskItemsByGuildIdAsync(123456789);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Title == "Test Task 1");
        Assert.Contains(result, t => t.Title == "Test Task 2");
    }
    
    [Fact]
    public async Task GetTaskItemByObjectIdOrDefaultAsync_ShouldReturnTaskItem_WhenTaskExists()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task",
            Description = "Test Task Description",
            AuthorId = 987654321
        };
        await _repository.CreateOrUpdateTaskItemAsync(task);

        // Act
        var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.GuildId, result.GuildId);
    }
    
    [Fact]
    public async Task GetTaskItemByObjectIdOrDefaultAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        // Act
        var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(ObjectId.GenerateNewId());

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task CreateOrUpdateTaskItemAsync_ShouldCreateTaskItem_WhenNotAlreadyCreated()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task",
            Description = "Test Task Description",
            AuthorId = 987654321
        };

        // Act
        await _repository.CreateOrUpdateTaskItemAsync(task);

        // Assert
        var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);
        Assert.NotNull(result);
        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.GuildId, result.GuildId);
        Assert.Equal(task.Title, result.Title);
        Assert.Equal(task.Description, result.Description);
        Assert.Equal(task.AuthorId, result.AuthorId);
    }
    
    [Fact]
    public async Task CreateOrUpdateTaskItemAsync_ShouldUpdateTaskItem_WhenAlreadyCreated()
    {
        // Arrange
        var originalTask = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task",
            Description = "Test Task Description",
            AuthorId = 987654321
        };
        
        var updatedTask = new TaskItem
        {
            Id = originalTask.Id,
            GuildId = originalTask.GuildId,
            Title = "Updated Test Task",
            Description = originalTask.Description,
            AuthorId = originalTask.AuthorId
        };
        
        await _repository.CreateOrUpdateTaskItemAsync(originalTask);
        
        // Act
        await _repository.CreateOrUpdateTaskItemAsync(updatedTask);

        var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(originalTask.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedTask.Id, result.Id);
        Assert.Equal(updatedTask.GuildId, result.GuildId);
        Assert.Equal(updatedTask.Title, result.Title);
        Assert.Equal(updatedTask.Description, result.Description);
        Assert.Equal(updatedTask.AuthorId, result.AuthorId);
    }
    
    [Fact]
    public async Task RemoveTaskItemAsync_ShouldRemoveTaskItem_WhenTaskItemExists()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task",
            Description = "Test Task Description",
            AuthorId = 987654321
        };
        
        await _repository.CreateOrUpdateTaskItemAsync(task);

        // Act
        await _repository.RemoveTaskItemAsync(task);

        var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task RemoveAllTaskItemsByIdAsync_ShouldRemoveAllTaskItemsById()
    {
        // Arrange
        var task1 = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task 1",
            Description = "Description 1",
            AuthorId = 987654321
        };
        
        var task2 = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            GuildId = 123456789,
            Title = "Test Task 2",
            Description = "Description 2",
            AuthorId = 987654322
        };

        await _repository.CreateOrUpdateTaskItemAsync(task1);
        await _repository.CreateOrUpdateTaskItemAsync(task2);

        // Act
        await _repository.RemoveAllTaskItemsByIdAsync(123456789);

        var result1 = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task1.Id);
        var result2 = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task2.Id);
        
        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
    }
    
    public void Dispose()
    {
        _runner.Dispose();
    }
}