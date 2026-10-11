using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.Tests.Factories;
using NetCord.Rest;

namespace KanbanCord.Tests.PresentationTests;

public class TaskViewTests
{
    private static readonly IReadOnlyDictionary<ulong, string> Names = new Dictionary<ulong, string>();

    private static TaskItem TaskWithComments(int comments)
    {
        var task = TaskItemFactory.Create();

        for (var i = 1; i <= comments; i++)
            task.Comments.Add(new Comment { AuthorId = 5, Text = $"Comment number {i}", CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) });

        return task;
    }

    private static List<IComponentContainerComponentProperties> ComponentsOf(InteractionMessageProperties message) =>
        Assert.IsType<ComponentContainerProperties>(Assert.Single(message.Components!)).Components.ToList();

    private static string AllText(InteractionMessageProperties message) =>
        string.Join("\n", ComponentsOf(message).OfType<TextDisplayProperties>().Select(x => x.Content));

    private static List<List<ButtonProperties>> ButtonRows(InteractionMessageProperties message) =>
        ComponentsOf(message)
            .OfType<ActionRowProperties>()
            .Select(row => row.Components.OfType<ButtonProperties>().ToList())
            .ToList();

    [Fact]
    public void PageCount_ShouldBeOnePlusCommentCount()
    {
        Assert.Equal(1, TaskView.PageCount(TaskWithComments(0)));
        Assert.Equal(4, TaskView.PageCount(TaskWithComments(3)));
    }

    [Fact]
    public void Build_ShouldOnlyOfferAddComment_WhenTaskHasNoComments()
    {
        // Act
        var message = TaskView.Build(TaskWithComments(0), Names, 0);

        // Assert
        var row = Assert.Single(ButtonRows(message));
        Assert.Single(row);
        Assert.Contains("Test Task", AllText(message));
    }

    [Fact]
    public void Build_ShouldOfferPagingAndAddComment_WhenTaskHasComments()
    {
        // Act
        var message = TaskView.Build(TaskWithComments(2), Names, 0);

        // Assert
        var rows = ButtonRows(message);
        Assert.Equal(2, rows.Count);
        Assert.Equal(3, rows[0].Count);
        Assert.Single(rows[1]);
    }

    [Fact]
    public void Build_ShouldShowTheCommentOfThatPage()
    {
        // Act
        var message = TaskView.Build(TaskWithComments(3), Names, 2);

        // Assert
        Assert.Contains("Comment number 2", AllText(message));
        Assert.DoesNotContain("Comment number 3", AllText(message));
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(99)]
    public void Build_ShouldClampOutOfRangePages_InsteadOfThrowing(int page)
    {
        // Act
        var message = TaskView.Build(TaskWithComments(2), Names, page);

        // Assert
        Assert.NotEmpty(AllText(message));
    }

    [Fact]
    public void Build_ShouldMentionTheCommentAuthor_SoNoNameLookupIsNeeded()
    {
        // Act
        var message = TaskView.Build(TaskWithComments(1), Names, 1);

        // Assert
        Assert.Contains("Comment by <@5>", AllText(message));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Build_ShouldNeverEmitAnEmptyActionRow(int comments)
    {
        // Arrange
        var task = TaskWithComments(comments);

        for (var page = 0; page < TaskView.PageCount(task); page++)
        {
            // Act
            var rows = ComponentsOf(TaskView.Build(task, Names, page)).OfType<ActionRowProperties>().ToList();

            // Assert
            Assert.NotEmpty(rows);
            Assert.All(rows, row => Assert.NotEmpty(row.Components));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Build_ShouldUseUniqueCustomIdsForEveryButton_OnEveryPage(int comments)
    {
        // Arrange
        var task = TaskWithComments(comments);

        for (var page = 0; page < TaskView.PageCount(task); page++)
        {
            // Act
            var ids = ComponentsOf(TaskView.Build(task, Names, page))
                .OfType<ActionRowProperties>()
                .SelectMany(row => row.Components)
                .OfType<ButtonProperties>()
                .Select(button => button.CustomId)
                .ToList();

            // Assert
            Assert.NotEmpty(ids);
            Assert.True(ids.Distinct().Count() == ids.Count, $"Duplicate custom ids on page {page} of {comments + 1}: {string.Join(", ", ids)}");
        }
    }
}
