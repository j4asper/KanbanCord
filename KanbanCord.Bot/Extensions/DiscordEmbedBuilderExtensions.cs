using DSharpPlus.Entities;

namespace KanbanCord.Bot.Extensions;

public static class DiscordEmbedBuilderExtensions
{
    public static DiscordEmbedBuilder WithDefaultColor(this DiscordEmbedBuilder embedBuilder)
    {
        return embedBuilder.WithColor(new DiscordColor(255, 117, 020));
    }
}