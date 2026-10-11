using KanbanCord.Core.Exceptions;
using KanbanCord.DiscordApplication.Presentation;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Rest;
using NetCord.Services;

namespace KanbanCord.Tests.PresentationTests;

public class ErrorMessageFactoryTests
{
    private static string TextOf(InteractionMessageProperties message)
    {
        var container = Assert.IsType<ComponentContainerProperties>(Assert.Single(message.Components!));

        return string.Join("\n", container.Components.OfType<TextDisplayProperties>().Select(x => x.Content));
    }

    [Fact]
    public void Create_ShouldShowKanbanCordExceptionMessage_AsEphemeral()
    {
        // Arrange
        var result = new ExecutionExceptionResult(new KanbanCordException("The selected task was not found, please try again."));

        // Act
        var message = ErrorMessageFactory.Create(result, null, NullLogger.Instance, "test");

        // Assert
        Assert.Equal("The selected task was not found, please try again.", TextOf(message));
        Assert.True(message.Flags!.Value.HasFlag(MessageFlags.Ephemeral));
        Assert.True(message.Flags!.Value.HasFlag(MessageFlags.IsComponentsV2));
    }

    [Fact]
    public void Create_ShouldHideUnexpectedExceptionDetails_AndOfferSupportLinkWhenConfigured()
    {
        // Arrange
        var result = new ExecutionExceptionResult(new InvalidOperationException("secret connection string"));

        // Act
        var message = ErrorMessageFactory.Create(result, "https://discord.gg/example", NullLogger.Instance, "test");

        // Assert
        Assert.DoesNotContain("secret", TextOf(message));
        var container = Assert.IsType<ComponentContainerProperties>(Assert.Single(message.Components!));
        Assert.Contains(container.Components, c => c is ActionRowProperties);
    }

    [Fact]
    public void Create_ShouldOmitSupportLink_WhenNotConfigured()
    {
        // Arrange
        var result = new ExecutionExceptionResult(new InvalidOperationException("boom"));

        // Act
        var message = ErrorMessageFactory.Create(result, null, NullLogger.Instance, "test");

        // Assert
        var container = Assert.IsType<ComponentContainerProperties>(Assert.Single(message.Components!));
        Assert.DoesNotContain(container.Components, c => c is ActionRowProperties);
    }

    [Fact]
    public void Create_ShouldShowPreconditionMessage()
    {
        // Arrange
        var result = new PreconditionFailResult("This button is only available to the user who used the command.");

        // Act
        var message = ErrorMessageFactory.Create(result, null, NullLogger.Instance, "test");

        // Assert
        Assert.Equal("This button is only available to the user who used the command.", TextOf(message));
        Assert.True(message.Flags!.Value.HasFlag(MessageFlags.Ephemeral));
    }
}
