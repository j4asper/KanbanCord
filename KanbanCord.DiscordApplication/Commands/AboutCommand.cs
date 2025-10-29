using System.Diagnostics;
using System.Runtime.InteropServices;
using KanbanCord.Core.Constants;
using KanbanCord.DiscordApplication.Extensions;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class AboutCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ShardedGatewayClient _shardedGatewayClient;
    private readonly TimeProvider _timeProvider;
    
    public AboutCommand(ShardedGatewayClient shardedGatewayClient, TimeProvider timeProvider)
    {
        _shardedGatewayClient = shardedGatewayClient;
        _timeProvider = timeProvider;
    }

    [SlashCommand("about", "Shows some stats and information about the bot")]
    public InteractionMessageProperties ExecuteCommand()
    {
        var shardCount = Context.Client.Shard!.Value.Count;
        var currentShard = Context.Client.Shard!.Value.Id;
        var guildCount = _shardedGatewayClient.Sum(x => x.Cache.Guilds.Count);
        var latency = _shardedGatewayClient.Average(x => x.Latency.TotalMilliseconds);
        var botVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        var dotnetVersion = RuntimeInformation.FrameworkDescription.Split(" ").Last();
        var memoryUsed = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
        var avatarUrl = Context.Client.Cache.User!.DefaultAvatarUrl.ToString();

        return new InteractionMessageProperties
        {
            Components =
            [
                new ComponentContainerProperties([
                    new ComponentSectionProperties(
                        new ComponentSectionThumbnailProperties(new ComponentMediaProperties(avatarUrl)),
                        [
                            new TextDisplayProperties("**What is KanbanCord?**\n**KanbanCord** is a powerful open-source Discord bot that integrates Kanban boards into your server for efficient task management and smooth team collaboration. Whether you're managing a project, organizing tasks, or planning events, KanbanCord makes it easy to track progress and stay organized. All within Discord!")
                        ]
                    ),
                    new ComponentSectionProperties(
                        new LinkButtonProperties(Source.RepositoryUrl, "Source Code", EmojiProperties.Standard("📦")),
                        [
                            new TextDisplayProperties($"**Open Source Project**\nLicensed under the [GPL-3.0]({Source.LicenseUrl}), you’re free to host it yourself, explore the code, and even contribute to its development by adding new features or fixing bugs. Check out the GitHub repository by clicking the button to the right.")
                        ]
                    ),
                    new ComponentSeparatorProperties(),
                    new TextDisplayProperties("### 📊 Stats"),
                    new TextDisplayProperties($"**Server Count:** {guildCount}"),
                    new TextDisplayProperties($"**Shard Count:** {shardCount}"),
                    new TextDisplayProperties($"**Uptime:** {GetUptimeString()}"),
                    new TextDisplayProperties($"**Current Shard ID:** {currentShard}"),
                    new TextDisplayProperties($"**Average Shard Latency:** {(int)latency} ms"),
                    new TextDisplayProperties($"**KanbanCord Version:** {botVersion}"),
                    new TextDisplayProperties($"**.NET Version:** {dotnetVersion}"),
                    new TextDisplayProperties($"**Memory Used:** {memoryUsed:F1} MB"),
                    new TextDisplayProperties("-# Made with 💗 using [C#](https://dotnet.microsoft.com/en-us/languages/csharp) and [NetCord](https://netcord.dev/)"),
                ])
                .WithDefaultColor()
            ],
            Flags = MessageFlags.IsComponentsV2
        };
    }
    
    private string GetUptimeString()
    {
        using var process  = Process.GetCurrentProcess();
        var uptime = _timeProvider.GetUtcNow().UtcDateTime.Subtract(process.StartTime.ToUniversalTime());
        var uptimeDays = uptime.Days;
        var remainingHours = uptime.Hours % 24;

        return $"{uptimeDays} days, {remainingHours} hours";
    }
}