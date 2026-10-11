using System.Diagnostics;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using Microsoft.Extensions.Caching.Memory;

namespace KanbanCord.Tests.ServiceTests;

public class UserNameResolverTests
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private UserNameResolver CreateResolver(Func<ulong, Task<string>> fetch, TimeSpan? timeout = null) =>
        new(fetch, _cache, timeout ?? TimeSpan.FromSeconds(5));

    [Fact]
    public async Task ResolveAsync_ShouldReturnNamesAndFetchEachDistinctUserOnce()
    {
        // Arrange
        var calls = 0;
        var resolver = CreateResolver(id => { Interlocked.Increment(ref calls); return Task.FromResult($"user{id}"); });

        // Act
        var result = await resolver.ResolveAsync([1UL, 2UL, 1UL]);

        // Assert
        Assert.Equal("user1", result[1]);
        Assert.Equal("user2", result[2]);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var calls = 0;
        var resolver = CreateResolver(id => { Interlocked.Increment(ref calls); return Task.FromResult($"user{id}"); });

        // Act
        await resolver.ResolveAsync([1UL]);
        await resolver.ResolveAsync([1UL]);

        // Assert
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ResolveAsync_ShouldShowUnknownUser_WhenFetchThrows()
    {
        // Arrange
        var resolver = CreateResolver(id => id == 2
            ? throw new HttpRequestException("network down")
            : Task.FromResult($"user{id}"));

        // Act
        var result = await resolver.ResolveAsync([1UL, 2UL]);

        // Assert
        Assert.Equal("user1", result[1]);
        Assert.Equal(BoardFormatter.UnknownUser, result[2]);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnWithinTimeout_AndKeepSlowUsersForLater()
    {
        // Arrange
        var release = new TaskCompletionSource<string>();
        var resolver = CreateResolver(id => id == 2 ? release.Task : Task.FromResult($"user{id}"), TimeSpan.FromMilliseconds(200));

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await resolver.ResolveAsync([1UL, 2UL]);
        stopwatch.Stop();

        // Assert: answered well before the slow fetch finished, with the fast result intact
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Took {stopwatch.Elapsed}");
        Assert.Equal("user1", result[1]);
        Assert.Equal(BoardFormatter.UnknownUser, result[2]);

        // The slow fetch still lands in the cache, so the next call is instant and correct
        release.SetResult("slowuser");
        await Task.Delay(100);
        var second = await resolver.ResolveAsync([2UL]);
        Assert.Equal("slowuser", second[2]);
    }
}
