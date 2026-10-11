using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Components;
using KanbanCord.DiscordApplication.Presentation;
using NetCord.Rest;
using KanbanCord.Tests.Factories;

namespace KanbanCord.Tests.PresentationTests;

public class TaskModalsTests
{
    private static StringMenuProperties PriorityMenu(ModalProperties modal) =>
        Assert.IsType<StringMenuProperties>(
            modal.Components.OfType<LabelProperties>()
                .Select(label => label.Component)
                .Single(component => component is StringMenuProperties));

    [Fact]
    public void Add_ShouldOfferLowMediumHighPriority_WithMediumPreselected()
    {
        // Act
        var menu = PriorityMenu(TaskModals.Add());

        // Assert
        Assert.Equal(InteractionIds.PriorityField, menu.CustomId);
        Assert.Equal(["Low", "Medium", "High"], menu.Options.Select(o => o.Label));
        Assert.Equal(["Low", "Medium", "High"], menu.Options.Select(o => o.Value));
        Assert.Equal("Medium", Assert.Single(menu.Options, o => o.Default).Value);
    }

    [Fact]
    public void Add_ShouldStillAskForTitleAndDescription()
    {
        // Act
        var inputs = TaskModals.Add().Components.OfType<LabelProperties>()
            .Select(label => label.Component).OfType<TextInputProperties>()
            .Select(input => input.CustomId).ToList();

        // Assert
        Assert.Equal([InteractionIds.TitleField, InteractionIds.DescriptionField], inputs);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    public void Edit_ShouldOfferPriority_WithTheTasksCurrentPriorityPreselected(Priority current)
    {
        // Arrange
        var task = new TaskItem { GuildId = 1, Title = "t", Description = "d", AuthorId = 1, Priority = current, CreatedAt = TestTimes.Default, LastUpdatedAt = TestTimes.Default };

        // Act
        var menu = PriorityMenu(TaskModals.Edit(task));

        // Assert
        Assert.Equal(InteractionIds.PriorityField, menu.CustomId);
        Assert.Equal(["Low", "Medium", "High"], menu.Options.Select(o => o.Value));
        Assert.Equal(current.ToString(), Assert.Single(menu.Options, o => o.Default).Value);
    }

    [Fact]
    public void Edit_ShouldPreselectMedium_WhenTheTaskHasNoPriority()
    {
        // Arrange
        var task = new TaskItem { GuildId = 1, Title = "t", Description = "d", AuthorId = 1, Priority = null, CreatedAt = TestTimes.Default, LastUpdatedAt = TestTimes.Default };

        // Act
        var menu = PriorityMenu(TaskModals.Edit(task));

        // Assert
        Assert.Equal("Medium", Assert.Single(menu.Options, o => o.Default).Value);
    }

    [Theory]
    [InlineData("Low", Priority.Low)]
    [InlineData("High", Priority.High)]
    public void ParsePriorityOrNull_ShouldReadTheSelectedValue(string value, Priority expected)
    {
        Assert.Equal(expected, TaskModals.ParsePriorityOrNull(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Urgent")]
    [InlineData("1")]
    public void ParsePriorityOrNull_ShouldReturnNull_ForMissingOrUnknownValues(string? value)
    {
        // Editing must keep the existing priority when the selection cannot be read
        Assert.Null(TaskModals.ParsePriorityOrNull(value));
    }

    [Theory]
    [InlineData("Low", Priority.Low)]
    [InlineData("Medium", Priority.Medium)]
    [InlineData("High", Priority.High)]
    public void ParsePriority_ShouldReadTheSelectedValue(string value, Priority expected)
    {
        Assert.Equal(expected, TaskModals.ParsePriority(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Urgent")]
    [InlineData("7")]
    [InlineData("-1")]
    public void ParsePriority_ShouldFallBackToMedium_ForMissingOrUnknownValues(string? value)
    {
        Assert.Equal(Priority.Medium, TaskModals.ParsePriority(value));
    }
}
