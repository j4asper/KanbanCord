using System.Reflection;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.Tests.ArchitectureTests;

public class CommandParityTests
{
    private static readonly Assembly Assembly = typeof(KanbanCord.DiscordApplication.Extensions.ServiceCollectionExtensions).Assembly;

    private static IEnumerable<(string Name, string Description)> RegisteredCommands()
    {
        foreach (var type in Assembly.GetTypes())
        {
            var group = type.GetCustomAttribute<SlashCommandAttribute>();

            if (group is not null)
                yield return (group.Name, group.Description);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var slash = method.GetCustomAttribute<SlashCommandAttribute>();

                if (slash is not null)
                    yield return (slash.Name, slash.Description);

                var sub = method.GetCustomAttribute<SubSlashCommandAttribute>();

                if (sub is not null)
                    yield return ($"{group?.Name}/{sub.Name}", sub.Description);
            }
        }
    }

    [Fact]
    public void RegisteredCommands_ShouldMatchTheOldBotsCommandSet()
    {
        // Arrange
        string[] expected =
        [
            "board", "archive", "clear", "reset", "help", "repository", "stats", "task",
            "task/add", "task/edit", "task/delete", "task/view", "task/start", "task/complete", "task/archive",
            "task/move", "task/assign", "task/me", "task/user", "task/priority", "task/comment",
        ];

        // Act
        var actual = RegisteredCommands().Select(c => c.Name).OrderBy(x => x).ToList();

        // Assert
        Assert.Equal(expected.OrderBy(x => x), actual);
    }

    [Fact]
    public void RegisteredCommands_ShouldRespectDiscordNameAndDescriptionLimits()
    {
        foreach (var (name, description) in RegisteredCommands())
        {
            var shortName = name.Split('/').Last();

            Assert.InRange(shortName.Length, 1, 32);
            Assert.Matches("^[a-z0-9_-]+$", shortName);
            Assert.InRange(description.Length, 1, 100);
        }
    }
}
