using KanbanCord.DiscordApplication.Presentation;
using Microsoft.Extensions.Caching.Memory;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Services;

public class UserNameResolver : IUserNameResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    // Discord requires an interaction to be answered within 3 seconds; leave room for Mongo and the response itself
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    private readonly Func<ulong, Task<string>> _fetchUsername;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _timeout;

    public UserNameResolver(RestClient restClient, IMemoryCache cache)
        : this(async userId => (await restClient.GetUserAsync(userId)).Username, cache, DefaultTimeout)
    {
    }

    internal UserNameResolver(Func<ulong, Task<string>> fetchUsername, IMemoryCache cache, TimeSpan timeout)
    {
        _fetchUsername = fetchUsername;
        _cache = cache;
        _timeout = timeout;
    }

    public async Task<IReadOnlyDictionary<ulong, string>> ResolveAsync(IEnumerable<ulong> userIds)
    {
        var ids = userIds.Distinct().ToArray();

        var lookups = ids.Select(ResolveOneAsync).ToArray();

        // Wait for all lookups, but never longer than the timeout. Lookups still in flight keep running and
        // fill the cache, so the next call is complete; this call shows "Unknown User" for the stragglers.
        await Task.WhenAny(Task.WhenAll(lookups), Task.Delay(_timeout));

        return ids
            .Zip(lookups)
            .ToDictionary(
                pair => pair.First,
                pair => pair.Second.IsCompletedSuccessfully ? pair.Second.Result : BoardFormatter.UnknownUser);
    }

    private async Task<string> ResolveOneAsync(ulong userId)
    {
        var key = $"username:{userId}";

        if (_cache.TryGetValue(key, out string? cached) && cached is not null)
            return cached;

        try
        {
            var username = await _fetchUsername(userId);

            _cache.Set(key, username, CacheDuration);

            return username;
        }
        catch (Exception)
        {
            // Deleted accounts, rate limits, network errors: one unresolvable author must not fail the whole board.
            // Not cached, so a transient failure heals on the next call.
            return BoardFormatter.UnknownUser;
        }
    }
}
