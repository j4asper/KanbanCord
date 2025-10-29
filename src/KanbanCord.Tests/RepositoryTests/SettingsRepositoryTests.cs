using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Repositories;
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
    public async Task GetSettingsByIdOrDefaultAsync_ShouldReturnSettings_WhenSettingExists()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };
        await _repository.CreateOrUpdateSettingsAsync(settings);

        // Act
        var result = await _repository.GetSettingsByIdOrDefaultAsync(123456789);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(settings.GuildId, result.GuildId);
        Assert.Equal(settings.ArchiveAfter, result.ArchiveAfter);
    }
    
    [Fact]
    public async Task GetSettingsByIdOrDefaultAsync_ShouldReturnNull_WhenSettingDoesNotExist()
    {
        // Act
        var result = await _repository.GetSettingsByIdOrDefaultAsync(987654321);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task CreateOrUpdateSettingsAsync_ShouldCreateSettings_WhenNotAlreadyCreated()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };

        // Act
        await _repository.CreateOrUpdateSettingsAsync(settings);

        var result = await _repository.GetSettingsByIdOrDefaultAsync(123456789);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(settings.GuildId, result.GuildId);
        Assert.Equal(settings.ArchiveAfter, result.ArchiveAfter);
    }
    
    [Fact]
    public async Task CreateOrUpdateSettingsAsync_ShouldUpdateSettings_WhenAlreadyCreated()
    {
        // Arrange
        var originalSettings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };
        var updatedSettings = new Settings { GuildId = originalSettings.GuildId, ArchiveAfter = TimeSpan.FromDays(3) };

        // Act
        await _repository.CreateOrUpdateSettingsAsync(originalSettings);

        await _repository.CreateOrUpdateSettingsAsync(updatedSettings);
        
        var result = await _repository.GetSettingsByIdOrDefaultAsync(123456789);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedSettings.GuildId, result.GuildId);
        Assert.Equal(updatedSettings.ArchiveAfter, result.ArchiveAfter);
    }

    [Fact]
    public async Task RemoveSettingsAsync_ShouldRemoveSettings_WhenSettingExists()
    {
        // Arrange
        var settings = new Settings { GuildId = 123456789, ArchiveAfter = TimeSpan.FromDays(7) };
        await _repository.CreateOrUpdateSettingsAsync(settings);

        // Act
        await _repository.RemoveSettingsAsync(123456789);

        var result = await _repository.GetSettingsByIdOrDefaultAsync(123456789);
        
        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _runner.Dispose();
    }
}