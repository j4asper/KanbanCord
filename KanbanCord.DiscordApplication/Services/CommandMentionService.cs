using Microsoft.Extensions.Caching.Memory;
using NetCord.Gateway;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Services;

public class CommandMentionService : ICommandMentionService
{
    private const string CacheKey = "application-commands";

    private readonly RestClient _restClient;
    private readonly ShardedGatewayClient _gatewayClient;
    private readonly IMemoryCache _cache;

    public CommandMentionService(RestClient restClient, ShardedGatewayClient gatewayClient, IMemoryCache cache)
    {
        _restClient = restClient;
        _gatewayClient = gatewayClient;
        _cache = cache;
    }

    public async ValueTask<string> GetMentionAsync(params string[] path)
    {
        var command = (await GetCommandsAsync()).FirstOrDefault(x => x.Name.Equals(path[0], StringComparison.OrdinalIgnoreCase));

        return command is null
            ? $"/{string.Join(' ', path)}"
            : $"</{string.Join(' ', path)}:{command.Id}>";
    }

    public async ValueTask<string> GetDescriptionAsync(params string[] path)
    {
        var command = (await GetCommandsAsync()).FirstOrDefault(x => x.Name.Equals(path[0], StringComparison.OrdinalIgnoreCase));

        if (command is null)
            return string.Empty;

        if (path.Length == 1)
            return command.Description;

        return command.Options?.FirstOrDefault(x => x.Name.Equals(path[1], StringComparison.OrdinalIgnoreCase))?.Description ?? string.Empty;
    }

    private async ValueTask<IReadOnlyList<ApplicationCommand>> GetCommandsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<ApplicationCommand>? commands) && commands is { Count: > 0 })
            return commands;

        commands = await _restClient.GetGlobalApplicationCommandsAsync(_gatewayClient.Id);

        // Only cache a populated list so a call made before commands are registered does not stick
        if (commands.Count > 0)
            _cache.Set(CacheKey, commands, TimeSpan.FromHours(1));

        return commands;
    }
}
