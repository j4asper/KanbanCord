using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using KanbanCord.Bot.Extensions;
using KanbanCord.Core.Constants;

namespace KanbanCord.Bot.Commands;

public class RepositoryCommand
{
    [Command("repository")]
    [Description("Get the repository url for this bot and other information.")]
    public async ValueTask ExecuteAsync(SlashCommandContext context)
    {
        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("KanbanCord Repository")
            .AddField("What is KanbanCord?",
                "**KanbanCord** is a powerful open-source Discord bot that integrates Kanban boards into your server for efficient task management and smooth team collaboration. Whether you're managing a project, organizing tasks, or planning events, KanbanCord makes it easy to track progress and stay organized. All within Discord!")
            .AddField("Open Source Project",
                $"Licensed under the [GPL-3.0]({Source.LicenseUrl}), you’re free to host it yourself, explore the code, and even contribute to its development by adding new features or fixing bugs. Check out the GitHub repository below to get started.");
        
        var response = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddActionRowComponent(
                new DiscordLinkButtonComponent(Source.RepositoryUrl, "Github Repository"),
                new DiscordLinkButtonComponent($"https://discord.com/oauth2/authorize?client_id={context.Client.CurrentUser.Id}", "Invite Bot"));
        
        await context.RespondAsync(response);
    }
}