using System.Reflection;
using KanbanCord.Core.Repositories;

namespace KanbanCord.Tests.ArchitectureTests;

public class NoRepositoryDependencyTests
{
    [Fact]
    public void DiscordApplication_ShouldNotInjectRepositories()
    {
        // Arrange
        var assembly = typeof(KanbanCord.DiscordApplication.Extensions.ServiceCollectionExtensions).Assembly;
        Type[] forbidden = [typeof(ITaskItemRepository), typeof(ISettingsRepository)];

        // Act
        var offenders = assembly.GetTypes()
            .SelectMany(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), (type, ctor) => (type, ctor))
            .Where(x => x.ctor.GetParameters().Any(p => forbidden.Contains(p.ParameterType)))
            .Select(x => x.type.FullName)
            .ToList();

        // Assert
        Assert.Empty(offenders);
    }

    [Fact]
    public void Core_ShouldNotReferenceNetCord()
    {
        // Arrange
        var references = typeof(KanbanCord.Core.Services.ITaskService).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert
        Assert.DoesNotContain(references, name => name!.StartsWith("NetCord", StringComparison.Ordinal));
    }
}
