using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Extensions;

namespace KanbanCord.Commands;

public class RepositoryCommand
{
    private const string RepositoryUrl = "https://github.com/j4asper/KanbanCord";
    private const string LicenseUrl = "https://github.com/j4asper/KanbanCord?tab=GPL-3.0-1-ov-file";
    
    [Command("repository")]
    [Description("Get the repository url for this bot and other information.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Repository")
            .WithDescription($"**KanbanCord** is an open-source Discord bot licensed under [GPL-3.0]({LicenseUrl}). You can host it yourself, explore the code, or contribute by adding features and fixing bugs. The github repository link is available below.");
        
        var response = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddComponents(new DiscordLinkButtonComponent(RepositoryUrl, "Github Repository"));
        
        await context.RespondAsync(response);
    }
}