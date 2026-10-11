using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using KanbanCord.Tests.Factories;
using Mongo2Go;
using MongoDB.Driver;

namespace KanbanCord.Tests.RepositoryTests;

public class SettingsRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;
    private readonly SettingsRepository _repository;

    public SettingsRepositoryTests()
    {
        _runner = MongoDbRunner.Start();

        _database = new MongoClient(_runner.ConnectionString)
            .GetDatabase("KanbanCord");
        
        _repository = new SettingsRepository(_database);
    }

    [Fact]
    public async Task AddAsync_ShouldAddSettings()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };

        // Act
        await _repository.AddAsync(settings);

        // Assert
        var result = await _repository.GetByIdOrDefaultAsync(123456789);
        Assert.NotNull(result);
        Assert.Equal(settings.GuildId, result.GuildId);
        Assert.Equal(settings.ArchiveAfter, result.ArchiveAfter);
    }

    [Fact]
    public async Task GetByIdOrDefaultAsync_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };
        await _repository.AddAsync(settings);

        // Act
        var result = await _repository.GetByIdOrDefaultAsync(123456789);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(settings.GuildId, result.GuildId);
        Assert.Equal(settings.ArchiveAfter, result.ArchiveAfter);
    }

    [Fact]
    public async Task GetByIdOrDefaultAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdOrDefaultAsync(987654321);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingSettings()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };
        await _repository.AddAsync(settings);

        settings.ArchiveAfter = TimeSpan.FromDays(14);

        // Act
        await _repository.UpdateAsync(settings);

        // Assert
        var result = await _repository.GetByIdOrDefaultAsync(123456789);
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromDays(14), result.ArchiveAfter);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveSettings()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };
        await _repository.AddAsync(settings);

        // Act
        await _repository.RemoveAsync(123456789);

        // Assert
        var result = await _repository.GetByIdOrDefaultAsync(123456789);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistNullArchiveAfter()
    {
        // Arrange
        var settings = SettingsFactory.Create();

        // Act
        await _repository.AddAsync(settings);
        var result = await _repository.GetByIdOrDefaultAsync(settings.GuildId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ArchiveAfter);
    }

    [Fact]
    public async Task GetByIdOrDefaultAsync_ShouldReturnOnlyTheRequestedGuild()
    {
        // Arrange
        foreach (var settings in SettingsFactory.CreateMany())
            await _repository.AddAsync(settings);

        // Act
        var result = await _repository.GetByIdOrDefaultAsync(3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3UL, result.GuildId);
        Assert.Equal(TimeSpan.FromDays(30), result.ArchiveAfter);
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrowOrAffectOthers_WhenSettingsDoNotExist()
    {
        // Arrange
        await _repository.AddAsync(SettingsFactory.Create(guildId: 1));

        // Act
        await _repository.RemoveAsync(999);

        // Assert
        Assert.NotNull(await _repository.GetByIdOrDefaultAsync(1));
    }

    public void Dispose()
    {
        _runner.Dispose();
    }
}