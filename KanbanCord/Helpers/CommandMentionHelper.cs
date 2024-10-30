using DSharpPlus.Entities;

namespace KanbanCord.Helpers;

public static class CommandMentionHelper
{
    public static string GetMention(this IReadOnlyList<DiscordApplicationCommand> applicationCommands, string[] commands)
    {
        if (commands.Length == 1)
            return applicationCommands.First(x => x.Name == commands.First()).Mention;
        
        return applicationCommands.First(x => x.Name == commands.First())
            .GetSubcommandMention(commands.Skip(1).ToArray());
    }
}