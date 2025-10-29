using KanbanCord.Core.Constants;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Extensions;

public static class ComponentContainerPropertiesExtensions
{
    public static ComponentContainerProperties WithDefaultColor(this ComponentContainerProperties properties)
    {
        return properties.WithAccentColor(ColorConstants.KanbanCord);
    }
}