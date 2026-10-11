using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class MessageFactory
{
    public static InteractionMessageProperties Text(string text, bool ephemeral = false) =>
        Container([new TextDisplayProperties(text)], ephemeral);

    public static InteractionMessageProperties Container(IEnumerable<IComponentContainerComponentProperties> components, bool ephemeral = false) =>
        new()
        {
            Components = [new ComponentContainerProperties(components).WithAccentColor(Colors.Default)],
            Flags = MessageFlags.IsComponentsV2 | (ephemeral ? MessageFlags.Ephemeral : 0),
            AllowedMentions = AllowedMentionsProperties.None,
        };

    public static InteractionMessageProperties WithAllowedUsers(this InteractionMessageProperties message, params ulong[] userIds)
    {
        message.AllowedMentions = new AllowedMentionsProperties { AllowedUsers = userIds };

        return message;
    }
}
