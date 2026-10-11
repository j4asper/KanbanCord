using KanbanCord.Core.Options;
using KanbanCord.DiscordApplication.Presentation;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class ComponentInteractionResultHandler<TContext> : IComponentInteractionResultHandler<TContext> where TContext : IComponentInteractionContext
{
    private readonly KanbanCordOptions _options;

    public ComponentInteractionResultHandler(IOptions<KanbanCordOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask HandleResultAsync(IExecutionResult result, TContext context, GatewayClient? client, ILogger logger, IServiceProvider services)
    {
        if (result is not IFailResult failResult)
            return;

        var message = ErrorMessageFactory.Create(failResult, _options.GetSupportInvite(), logger, $"interaction '{context.Interaction.Data.CustomId}'");

        await context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
    }
}
