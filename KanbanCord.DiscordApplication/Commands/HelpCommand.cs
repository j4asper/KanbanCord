using KanbanCord.Core.Options;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class HelpCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private static readonly string[][] Commands =
    [
        ["board"], ["archive"], ["clear"], ["reset"], ["repository"], ["stats"],
        ["task", "add"], ["task", "edit"], ["task", "delete"], ["task", "view"], ["task", "start"],
        ["task", "complete"], ["task", "archive"], ["task", "move"], ["task", "assign"], ["task", "me"],
        ["task", "user"], ["task", "priority"], ["task", "comment"],
    ];

    private readonly ICommandMentionService _commandMentionService;
    private readonly KanbanCordOptions _options;

    public HelpCommand(ICommandMentionService commandMentionService, IOptions<KanbanCordOptions> options)
    {
        _commandMentionService = commandMentionService;
        _options = options.Value;
    }

    [SlashCommand("help", "Displays all the commands available.", Contexts = [InteractionContextType.Guild])]
    public async Task<InteractionMessageProperties> HelpAsync()
    {
        List<IComponentContainerComponentProperties> components = [new TextDisplayProperties("### KanbanCord Commands")];

        foreach (var path in Commands)
        {
            var mention = await _commandMentionService.GetMentionAsync(path);
            var description = await _commandMentionService.GetDescriptionAsync(path);

            components.Add(new TextDisplayProperties($"{mention}\n{description}"));
        }

        List<IActionRowComponentProperties> buttons = [new LinkButtonProperties($"https://discord.com/oauth2/authorize?client_id={Context.Client.Id}", "Invite")];

        if (_options.GetSupportInvite() is { } supportInvite)
            buttons.Add(new LinkButtonProperties(supportInvite, "Support"));

        components.Add(new ActionRowProperties(buttons));

        return MessageFactory.Container(components);
    }
}
