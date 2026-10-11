using System.Text.RegularExpressions;
using KanbanCord.Core.Exceptions;
using NetCord;
using NetCord.Rest;
using NetCord.Services;

namespace KanbanCord.DiscordApplication.Presentation;

public static partial class ErrorMessageFactory
{
    public static InteractionMessageProperties Create(IFailResult result, string? supportInvite, ILogger logger, string source)
    {
        if (result is IExceptionResult { Exception: KanbanCordException known })
            return MessageFactory.Text(known.Message, ephemeral: true);

        if (result is MissingPermissionsResult missing)
        {
            var permissions = Enum.GetValues<Permissions>()
                .Where(permission => permission != 0 && missing.MissingPermissions.HasFlag(permission))
                .Select(permission => $"`{SpacedWords().Replace(permission.ToString(), " $1").Trim()}`");

            var subject = missing.EntityType == MissingPermissionsResultEntityType.Bot ? "I am" : "You are";

            return MessageFactory.Text($"{subject} missing the following permissions: {string.Join(", ", permissions)}", ephemeral: true);
        }

        if (result is IExceptionResult exceptionResult)
            logger.LogError(exceptionResult.Exception, "Execution of {Source} failed with an exception", source);
        else
            logger.LogDebug("Execution of {Source} failed with '{Message}'", source, result.Message);

        if (result is PreconditionFailResult precondition)
            return MessageFactory.Text(precondition.Message, ephemeral: true);

        List<IComponentContainerComponentProperties> components =
        [
            new TextDisplayProperties("Something went wrong while handling that. Please try again, and if the problem persists, contact support."),
        ];

        if (!string.IsNullOrWhiteSpace(supportInvite))
            components.Add(new ActionRowProperties([new LinkButtonProperties(supportInvite, "Support")]));

        return MessageFactory.Container(components, ephemeral: true);
    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex SpacedWords();
}
