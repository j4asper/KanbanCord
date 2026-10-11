using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using KanbanCord.Tests.Factories;
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

        [Fact]
        public async Task AddTaskItemAsync_ShouldGenerateId_WhenIdIsEmpty()
        {
            // Arrange
            var task = TaskItemFactory.Create();
            Assert.Equal(ObjectId.Empty, task.Id);

            // Act
            await _repository.AddTaskItemAsync(task);

            // Assert
            Assert.NotEqual(ObjectId.Empty, task.Id);
        }

        [Fact]
        public async Task AddTaskItemAsync_ShouldPersistEveryField()
        {
            // Arrange
            var task = TaskItemFactory.Create(assigneeId: 555555555555555555);
            task.Priority = Priority.High;
            task.Comments.Add(new Comment
            {
                AuthorId = 444444444444444444,
                Text = "A persisted comment",
                CreatedAt = new DateTime(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc),
            });

            // Act
            await _repository.AddTaskItemAsync(task);
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TaskItemFactory.DefaultGuildId, result.GuildId);
            Assert.Equal(task.AuthorId, result.AuthorId);
            Assert.Equal(555555555555555555UL, result.AssigneeId);
            Assert.Equal(Priority.High, result.Priority);
            Assert.Equal(BoardStatus.Backlog, result.Status);
            Assert.Equal(task.CreatedAt, result.CreatedAt);
            var comment = Assert.Single(result.Comments);
            Assert.Equal(444444444444444444UL, comment.AuthorId);
            Assert.Equal("A persisted comment", comment.Text);
            Assert.Equal(new DateTime(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc), comment.CreatedAt);
        }

        [Fact]
        public async Task GetAllTaskItemsByGuildIdAsync_ShouldReturnEmptyList_WhenGuildHasNoTasks()
        {
            // Arrange
            await _repository.AddTaskItemAsync(TaskItemFactory.Create(guildId: 1));

            // Act
            var result = await _repository.GetAllTaskItemsByGuildIdAsync(2);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllTaskItemsByGuildIdAsync_ShouldReturnTasksInInsertionOrder()
        {
            // Arrange
            var titles = Enumerable.Range(1, 25).Select(i => $"Task {i}").ToList();

            foreach (var title in titles)
                await _repository.AddTaskItemAsync(TaskItemFactory.Create(title: title));

            // Act
            var result = await _repository.GetAllTaskItemsByGuildIdAsync(TaskItemFactory.DefaultGuildId);

            // Assert
            Assert.Equal(titles, result.Select(x => x.Title));
        }

        [Fact]
        public async Task UpdateTaskItemAsync_ShouldNotInsertTask_WhenTaskDoesNotExist()
        {
            // Arrange
            var task = TaskItemFactory.Create();
            task.Id = ObjectId.GenerateNewId();

            // Act
            await _repository.UpdateTaskItemAsync(task);

            // Assert
            Assert.Empty(await _repository.GetAllTaskItemsByGuildIdAsync(task.GuildId));
        }

        [Fact]
        public async Task RemoveAllTaskItemsByIdAsync_ShouldNotRemoveTasksOfOtherGuilds()
        {
            // Arrange
            foreach (var task in TaskItemFactory.CreateMany())
                await _repository.AddTaskItemAsync(task);

            // Act
            await _repository.RemoveAllTaskItemsByIdAsync(TaskItemFactory.DefaultGuildId);

            // Assert
            Assert.Empty(await _repository.GetAllTaskItemsByGuildIdAsync(TaskItemFactory.DefaultGuildId));
            Assert.Single(await _repository.GetAllTaskItemsByGuildIdAsync(1));
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}
