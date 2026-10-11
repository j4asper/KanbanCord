using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.Tests.Factories;

namespace KanbanCord.Tests.PresentationTests;

public class BoardFormatterTests
{
    private static BoardEntry Entry(int number, string title = "Task", ulong authorId = 1) =>
        new(number, new TaskItem { GuildId = 1, Title = title, Description = "d", AuthorId = authorId, CreatedAt = TestTimes.Default, LastUpdatedAt = TestTimes.Default });

    [Fact]
    public void FormatColumn_ShouldRenderPlaceholder_WhenColumnIsEmpty()
    {
        // Act
        var result = BoardFormatter.FormatColumn([], _ => "x", 1000);

        // Assert
        Assert.Equal("```bash\n ```", result);
    }

    [Fact]
    public void FormatColumn_ShouldRenderNumberTitleAndAuthor()
    {
        // Act
        var result = BoardFormatter.FormatColumn([Entry(1, "Fix bug", 7), Entry(2, "Ship it", 8)], id => $"user{id}", 1000);

        // Assert
        Assert.Equal("```bash\n1 - \"Fix bug\" added by: user7\n2 - \"Ship it\" added by: user8```", result);
    }

    [Fact]
    public void FormatColumn_ShouldTruncateWithRemainingCount_WhenTooLong()
    {
        // Arrange
        var entries = Enumerable.Range(1, 200).Select(i => Entry(i, new string('t', 40))).ToList();

        // Act
        var result = BoardFormatter.FormatColumn(entries, _ => "someone", 500);

        // Assert
        Assert.True(result.Length <= 500, $"Length was {result.Length}");
        Assert.Matches(@"…and \d+ more```$", result);
        Assert.StartsWith("```bash\n1 - ", result);
    }

    [Fact]
    public void FormatColumn_ShouldNeverExceedMax_ForSingleColumnLimit()
    {
        // Arrange
        var entries = Enumerable.Range(1, 500).Select(i => Entry(i, new string('t', 40))).ToList();

        // Act
        var result = BoardFormatter.FormatColumn(entries, _ => new string('n', 32), BoardView.SingleColumnMaxLength);

        // Assert
        Assert.True(result.Length <= BoardView.SingleColumnMaxLength);
    }

    [Fact]
    public void FormatColumn_ShouldShowUnknownUser_WhenNameCannotBeResolved()
    {
        // Act
        var result = BoardFormatter.FormatColumn([Entry(1, "Task", 99)], _ => BoardFormatter.UnknownUser, 1000);

        // Assert
        Assert.Contains("added by: Unknown User", result);
    }
}
