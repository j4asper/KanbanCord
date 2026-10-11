using KanbanCord.Core.Constants;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class RepositoryCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("repository", "Get the repository url for this bot and other information.", Contexts = [InteractionContextType.Guild])]
    public InteractionMessageProperties Repository() =>
        MessageFactory.Container(
        [
            new TextDisplayProperties("### KanbanCord Repository"),
            new TextDisplayProperties(
                "**What is KanbanCord?**\n**KanbanCord** is a powerful open-source Discord bot that integrates Kanban boards into your server for efficient task management and smooth team collaboration. Whether you're managing a project, organizing tasks, or planning events, KanbanCord makes it easy to track progress and stay organized. All within Discord!"),
            new TextDisplayProperties(
                $"**Open Source Project**\nLicensed under the [GPL-3.0]({Source.LicenseUrl}), you’re free to host it yourself, explore the code, and even contribute to its development by adding new features or fixing bugs. Check out the GitHub repository below to get started."),
            new ActionRowProperties(
            [
                new LinkButtonProperties(Source.RepositoryUrl, "Github Repository"),
                new LinkButtonProperties($"https://discord.com/oauth2/authorize?client_id={Context.Client.Id}", "Invite Bot"),
            ]),
        ]);
}
