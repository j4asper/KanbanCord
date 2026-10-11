using KanbanCord.DiscordApplication.Components;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class ResetCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SlashCommand("reset", "Resets the kanban board completely, this will delete all current and archived tasks.", Contexts = [InteractionContextType.Guild])]
    public InteractionMessageProperties Reset() =>
        MessageFactory.Container(
        [
            new TextDisplayProperties("### Reset Board"),
            new TextDisplayProperties("Are you sure you want to reset the board? This can not be undone!"),
            new ActionRowProperties(
            [
                new ButtonProperties(InteractionIds.ResetConfirm, "Reset", ButtonStyle.Danger),
                new ButtonProperties(InteractionIds.ResetCancel, "Cancel", ButtonStyle.Secondary),
            ]),
        ]);
}
