using KanbanCord.Core.Options;
using KanbanCord.DiscordApplication.Presentation;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class ApplicationCommandResultHandler<TContext> : IApplicationCommandResultHandler<TContext> where TContext : IApplicationCommandContext
{
    private readonly KanbanCordOptions _options;

    public ApplicationCommandResultHandler(IOptions<KanbanCordOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask HandleResultAsync(IExecutionResult result, TContext context, GatewayClient? client, ILogger logger, IServiceProvider services)
    {
        if (result is not IFailResult failResult)
            return;

        var message = ErrorMessageFactory.Create(failResult, _options.GetSupportInvite(), logger, $"command '{context.Interaction.Data.Name}'");

        await context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
    }
}
