using NetCord.Services;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Attributes;

/// <summary>Only the user who ran the original command may press buttons on its response.</summary>
public class RequireInteractionOwnerAttribute<TContext> : PreconditionAttribute<TContext> where TContext : ButtonInteractionContext
{
    public override ValueTask<PreconditionResult> EnsureCanExecuteAsync(TContext context, IServiceProvider? serviceProvider)
    {
        if (context.Interaction.Message.InteractionMetadata is not null)
        {
            if (context.Interaction.Message.InteractionMetadata.User.Id != context.User.Id)
                return new(PreconditionResult.Fail("This button is only available to the user who used the command."));
        }

        return new(PreconditionResult.Success);
    }
}
