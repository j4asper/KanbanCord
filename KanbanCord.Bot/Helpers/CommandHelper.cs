using DSharpPlus.Entities;

namespace KanbanCord.Bot.Helpers;

public static class CommandHelper
{
    public static string GetMention(this IReadOnlyList<DiscordApplicationCommand> applicationCommands, string[] commands)
    {
        if (commands.Length == 1)
            return applicationCommands.First(x => x.Name == commands.First()).Mention;
        
        return applicationCommands.First(x => x.Name == commands.First())
            .GetSubcommandMention(commands.Skip(1).ToArray());
    }
    
    public static string GetDescription(this IReadOnlyList<DiscordApplicationCommand> applicationCommands, string[] commands)
    {
        if (commands.Length == 1)
            return applicationCommands.First(x => x.Name == commands.First()).Description;
        
        return applicationCommands.First(x => x.Name == commands.First())
            .Options.First(x => x.Name == commands[1]).Description;
    }
}