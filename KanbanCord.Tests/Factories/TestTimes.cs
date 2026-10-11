namespace KanbanCord.Tests.Factories;

public static class TestTimes
{
    /// <summary>A fixed UTC instant for test data, so tests never depend on the wall clock.</summary>
    public static readonly DateTime Default = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
}
