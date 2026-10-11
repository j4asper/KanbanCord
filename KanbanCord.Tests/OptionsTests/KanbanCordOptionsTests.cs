using KanbanCord.Core.Options;

namespace KanbanCord.Tests.OptionsTests;

public class KanbanCordOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSupportInvite_ShouldReturnNull_WhenNotConfigured(string? value)
    {
        // Arrange
        var options = new KanbanCordOptions { SupportInvite = value };

        // Act & Assert
        Assert.Null(options.GetSupportInvite());
    }

    [Fact]
    public void GetSupportInvite_ShouldReturnTrimmedInvite_WhenConfigured()
    {
        // Arrange
        var options = new KanbanCordOptions { SupportInvite = "  https://discord.gg/example  " };

        // Act & Assert
        Assert.Equal("https://discord.gg/example", options.GetSupportInvite());
    }
}
