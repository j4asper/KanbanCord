using System.Diagnostics;
using System.Reflection;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class StatsCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ShardedGatewayClient _gatewayClient;

    public StatsCommand(ShardedGatewayClient gatewayClient)
    {
        _gatewayClient = gatewayClient;
    }

    [SlashCommand("stats", "Displays some stats of the bot.", Contexts = [InteractionContextType.Guild])]
    public InteractionMessageProperties Stats()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        var guildCount = _gatewayClient.Sum(shard => shard.Cache.Guilds.Count);

        var heapMemory = $"{GC.GetTotalMemory(false) / 1024 / 1024:n0} MB";

        using var process = Process.GetCurrentProcess();
        var uptime = DateTimeOffset.UtcNow - process.StartTime;

        var latency = (int)Context.Client.Latency.TotalMilliseconds;

        return MessageFactory.Container(
        [
            new TextDisplayProperties("### KanbanCord Stats"),
            new TextDisplayProperties($"**Latency:** {(latency == 0 ? "waiting for heartbeat..." : $"{latency} ms")}"),
            new TextDisplayProperties($"**Servers:** {guildCount}"),
            new TextDisplayProperties($"**Memory Usage:** {heapMemory}"),
            new TextDisplayProperties($"**Uptime:** {uptime.Days} days, {uptime.Hours} hours"),
            new TextDisplayProperties($"**Bot Version:** v{version.Major}.{version.Minor}.{version.Build}"),
        ]);
    }
}
