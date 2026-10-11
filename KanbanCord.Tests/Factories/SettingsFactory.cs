using KanbanCord.Core.Models;

namespace KanbanCord.Tests.Factories;

public static class SettingsFactory
{
    public static Settings Create(ulong guildId = TaskItemFactory.DefaultGuildId, TimeSpan? archiveAfter = null) =>
        new() { GuildId = guildId, ArchiveAfter = archiveAfter };

    public static IReadOnlyList<Settings> CreateMany() =>
    [
        Create(1, TimeSpan.FromDays(7)),
        Create(2),
        Create(3, TimeSpan.FromDays(30)),
    ];
}
