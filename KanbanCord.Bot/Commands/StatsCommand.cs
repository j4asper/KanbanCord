using System.ComponentModel;
using System.Diagnostics;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Helpers;

namespace KanbanCord.Bot.Commands;

public class StatsCommand
{
    [Command("stats")]
    [Description("Displays some stats of the bot.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var botVersion = EnvironmentHelpers.GetApplicationVersion();
        
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        var heapMemory = $"{GC.GetTotalMemory(true) / 1024 / 1024:n0} MB";

        using var process  = Process.GetCurrentProcess();
        var uptime = DateTimeOffset.UtcNow.Subtract(process.StartTime);
        var uptimeDays = uptime.Days;
        var remainingHours = uptime.Hours % 24;
        
        var latency = context.Client.GetConnectionLatency(context.Guild!.Id).Milliseconds;
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithThumbnail(context.Client.CurrentUser.AvatarUrl)
            .AddField("Latency:", latency == 0 ? "waiting for heartbeat..." : $"{latency} ms")
            .AddField("Memory Usage:", heapMemory)
            .AddField("Uptime:", $"{uptimeDays} days, {remainingHours} hours")
            .AddField("Bot Version:", botVersion);
        
        await context.RespondAsync(embed);
    }
}