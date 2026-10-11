using KanbanCord.DiscordApplication.Components;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class ClearCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SlashCommand("clear", "Clear the kanban board completely, this will archive all current tasks.", Contexts = [InteractionContextType.Guild])]
    public InteractionMessageProperties Clear() =>
        MessageFactory.Container(
        [
            new TextDisplayProperties("### Clear Board"),
            new TextDisplayProperties("Are you sure you want to clear the board?"),
            new ActionRowProperties(
            [
                new ButtonProperties(InteractionIds.ClearConfirm, "Clear", ButtonStyle.Danger),
                new ButtonProperties(InteractionIds.ClearCancel, "Cancel", ButtonStyle.Secondary),
            ]),
        ]);
}
