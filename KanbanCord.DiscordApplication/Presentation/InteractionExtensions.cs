using KanbanCord.Core.Exceptions;
using NetCord;

namespace KanbanCord.DiscordApplication.Presentation;

public static class InteractionExtensions
{
    // Commands are guild-only; this is defence in depth for interactions that arrive without a guild
    public static ulong RequireGuildId(this Interaction interaction) =>
        interaction.GuildId ?? throw new KanbanCordException("This command can only be used in a server.");
}
