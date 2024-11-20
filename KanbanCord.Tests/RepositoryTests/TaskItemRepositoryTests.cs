using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using Mongo2Go;
using MongoDB.Driver;
using MongoDB.Bson;

namespace KanbanCord.Tests.RepositoryTests
{
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
        public async Task AddTaskItemAsync_ShouldAddTaskItem()
        {
            // Arrange
            var task = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task",
                Description = "Test Task Description",
                AuthorId = 987654321
            };

            // Act
            await _repository.AddTaskItemAsync(task);

            // Assert
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);
            Assert.NotNull(result);
            Assert.Equal(task.GuildId, result.GuildId);
            Assert.Equal(task.Title, result.Title);
            Assert.Equal(task.Description, result.Description);
            Assert.Equal(task.AuthorId, result.AuthorId);
        }

        [Fact]
        public async Task GetAllTaskItemsByGuildIdAsync_ShouldReturnTaskItems()
        {
            // Arrange
            var task1 = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task 1",
                Description = "Description 1",
                AuthorId = 987654321
            };
            var task2 = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task 2",
                Description = "Description 2",
                AuthorId = 987654322
            };

            await _repository.AddTaskItemAsync(task1);
            await _repository.AddTaskItemAsync(task2);

            // Act
            var result = await _repository.GetAllTaskItemsByGuildIdAsync(123456789);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Title == "Test Task 1");
            Assert.Contains(result, t => t.Title == "Test Task 2");
        }

        [Fact]
        public async Task GetTaskItemByObjectIdOrDefaultAsync_ShouldReturnTaskItem_WhenExists()
        {
            // Arrange
            var task = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task",
                Description = "Test Task Description",
                AuthorId = 987654321
            };
            await _repository.AddTaskItemAsync(task);

            // Act
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(task.Id, result?.Id);
            Assert.Equal(task.GuildId, result?.GuildId);
        }

        [Fact]
        public async Task GetTaskItemByObjectIdOrDefaultAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(ObjectId.GenerateNewId());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTaskItemAsync_ShouldUpdateTaskItem()
        {
            // Arrange
            var task = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task",
                Description = "Test Task Description",
                AuthorId = 987654321
            };
            await _repository.AddTaskItemAsync(task);

            task.Description = "Updated Task Description";

            // Act
            await _repository.UpdateTaskItemAsync(task);

            // Assert
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);
            Assert.NotNull(result);
            Assert.Equal("Updated Task Description", result?.Description);
        }

        [Fact]
        public async Task RemoveTaskItemAsync_ShouldRemoveTaskItem()
        {
            // Arrange
            var task = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task",
                Description = "Test Task Description",
                AuthorId = 987654321
            };
            await _repository.AddTaskItemAsync(task);

            // Act
            await _repository.RemoveTaskItemAsync(task);

            // Assert
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveAllTaskItemsByIdAsync_ShouldRemoveAllTaskItemsByGuildId()
        {
            // Arrange
            var task1 = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task 1",
                Description = "Description 1",
                AuthorId = 987654321
            };
            var task2 = new TaskItem
            {
                GuildId = 123456789,
                Title = "Test Task 2",
                Description = "Description 2",
                AuthorId = 987654322
            };

            await _repository.AddTaskItemAsync(task1);
            await _repository.AddTaskItemAsync(task2);

            // Act
            await _repository.RemoveAllTaskItemsByIdAsync(123456789);

            // Assert
            var result1 = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task1.Id);
            var result2 = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task2.Id);

            Assert.Null(result1);
            Assert.Null(result2);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}
