using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;

namespace KanbanCord.Bot.Commands;

public class StatsCommand
{
    [Command("stats")]
    [Description("Displays some stats of the bot.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        
        var guildCount = context.Client.Guilds.Count;
        
        var heapMemory = $"{GC.GetTotalMemory(false) / 1024 / 1024:n0} MB";

        using var process  = Process.GetCurrentProcess();
        var uptime = DateTimeOffset.UtcNow.Subtract(process.StartTime);
        var uptimeDays = uptime.Days;
        var remainingHours = uptime.Hours % 24;
        
        var latency = context.Client.GetConnectionLatency(context.Guild!.Id).Milliseconds;
        
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithThumbnail(context.Client.CurrentUser.AvatarUrl)
            .AddField("Latency:", latency == 0 ? "waiting for heartbeat..." : $"{latency} ms")
            .AddField("Servers:", guildCount.ToString())
            .AddField("Memory Usage:", heapMemory)
            .AddField("Uptime:", $"{uptimeDays} days, {remainingHours} hours")
            .AddField("Bot Version:", $"v{version.Major}.{version.Minor}.{version.Build}");
        
        await context.RespondAsync(embed);
    }
}