using KanbanCord.Core.Repositories;
using KanbanCord.Core.Services;
using NSubstitute;

namespace KanbanCord.Tests.ServiceTests;

public class SettingsServiceTests
{
    [Fact]
    public async Task RemoveGuildDataAsync_ShouldRemoveTasksAndSettingsOfThatGuild()
    {
        // Arrange
        var taskService = Substitute.For<ITaskService>();
        var settingsRepository = Substitute.For<ISettingsRepository>();
        var service = new SettingsService(taskService, settingsRepository);

        // Act
        await service.RemoveGuildDataAsync(123);

        // Assert
        await taskService.Received(1).ResetBoardAsync(123);
        await settingsRepository.Received(1).RemoveAsync(123);
        Assert.Single(taskService.ReceivedCalls());
        Assert.Single(settingsRepository.ReceivedCalls());
    }
}
