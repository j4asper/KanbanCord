using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Extensions;
using KanbanCord.Helpers;

namespace KanbanCord.Commands;

public class HelpCommand
{
    [Command("help")]
    [Description("Displays all the commands available.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var commands = await context.Client.GetGlobalApplicationCommandsAsync();

        var commandDescriptions = new List<(string CommandMention, string Description)>
        {
            (commands.GetMention(["board"]), 
                "Shows the Kanban board items."),
        };

        var pages = new List<Page>();
        
        var chunkedCommandDescriptions = commandDescriptions.Chunk(6);

        foreach (var chunkedCommandDescriptionsArray in chunkedCommandDescriptions)
        {
            var embedPage = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithAuthor("KanbanCord Commands");
            
            chunkedCommandDescriptionsArray.ToList()
                .ForEach(commandDescription => embedPage.AddField(commandDescription.CommandMention, commandDescription.Description));
            
            pages.Add(new Page(string.Empty, embedPage));
        }
        
        await context.SendSimplePaginatedMessage(pages);
    }
}