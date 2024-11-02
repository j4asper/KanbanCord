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
    private const string BaseInviteUrl = "https://discord.com/oauth2/authorize?client_id=";
    
    [Command("help")]
    [Description("Displays all the commands available.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var commands = await context.Client.GetGlobalApplicationCommandsAsync();
        
        var commandDescriptions = new List<(string CommandMention, string Description)>
        {
            (commands.GetMention(["board"]), commands.GetDescription(["board"])),
            (commands.GetMention(["archive"]), commands.GetDescription(["archive"])),
            (commands.GetMention(["repository"]), commands.GetDescription(["repository"])),
            (commands.GetMention(["task", "add"]), commands.GetDescription(["task", "add"])),
            (commands.GetMention(["task", "edit"]), commands.GetDescription(["task", "edit"])),
            (commands.GetMention(["task", "delete"]), commands.GetDescription(["task", "delete"])),
            (commands.GetMention(["task", "view"]), commands.GetDescription(["task", "view"])),
            (commands.GetMention(["task", "start"]), commands.GetDescription(["task", "start"])),
            (commands.GetMention(["task", "complete"]), commands.GetDescription(["task", "complete"])),
            (commands.GetMention(["task", "archive"]), commands.GetDescription(["task", "archive"])),
            (commands.GetMention(["task", "move"]), commands.GetDescription(["task", "move"]))
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

        List<DiscordComponent> additionalComponents = [new DiscordLinkButtonComponent(BaseInviteUrl + context.Client.CurrentUser.Id, "Invite")];
        
        await context.SendSimplePaginatedMessage(pages, additionalComponents);
    }
}