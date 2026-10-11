using System.Reflection;
using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Components;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.Tests.Factories;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.Tests.PresentationTests;

public class ComponentRoutingContractTests
{
    private static int HandlerParameterCount(Type module, string customIdPrefix) =>
        module.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(method => method.GetCustomAttribute<ComponentInteractionAttribute>()?.CustomId == customIdPrefix)
            .GetParameters()
            .Length;

    private static IEnumerable<string> ButtonIds(InteractionMessageProperties message) =>
        Assert.IsType<ComponentContainerProperties>(Assert.Single(message.Components!)).Components
            .OfType<ActionRowProperties>()
            .SelectMany(row => row.Components)
            .OfType<ButtonProperties>()
            .Select(button => button.CustomId);

    [Fact]
    public void TaskViewPagingButtons_ShouldCarryExactlyTheValuesTheHandlerExpects()
    {
        // Arrange
        var task = TaskItemFactory.Create();
        for (var i = 1; i <= 3; i++)
            task.Comments.Add(new Comment { AuthorId = 5, Text = $"Comment number {i}" });

        var expected = HandlerParameterCount(typeof(TaskViewHandlers), InteractionIds.TaskViewPage);

        // Act
        var pagingIds = ButtonIds(TaskView.Build(task, new Dictionary<ulong, string>(), 1))
            .Where(id => id.StartsWith($"{InteractionIds.TaskViewPage}:"))
            .ToList();

        // Assert
        Assert.Equal(2, pagingIds.Count);
        Assert.All(pagingIds, id => Assert.Equal(expected, id.Split(':').Length - 1));
    }

    [Fact]
    public void TaskCommentButton_ShouldCarryExactlyTheValuesTheHandlerExpects()
    {
        // Arrange
        var task = TaskItemFactory.Create();
        var expected = HandlerParameterCount(typeof(TaskButtonHandlers), InteractionIds.TaskCommentButton);

        // Act
        var id = ButtonIds(TaskView.Build(task, new Dictionary<ulong, string>(), 0))
            .Single(id => id.StartsWith($"{InteractionIds.TaskCommentButton}:"));

        // Assert
        Assert.Equal(expected, id.Split(':').Length - 1);
    }
}
