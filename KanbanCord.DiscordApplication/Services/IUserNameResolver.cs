namespace KanbanCord.DiscordApplication.Services;

public interface IUserNameResolver
{
    /// <summary>Unknown or unresolvable users map to <c>BoardFormatter.UnknownUser</c>.</summary>
    Task<IReadOnlyDictionary<ulong, string>> ResolveAsync(IEnumerable<ulong> userIds);
}
