using MongoDB.Bson;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.TypeReaders;

public class ObjectIdTypeReader<TContext> : ComponentInteractionTypeReader<TContext> where TContext : IComponentInteractionContext
{
    public override ValueTask<ComponentInteractionTypeReaderResult> ReadAsync(ReadOnlyMemory<char> input, TContext context, ComponentInteractionParameter<TContext> parameter,
        ComponentInteractionServiceConfiguration<TContext> configuration, IServiceProvider? serviceProvider)
    {
        return new ValueTask<ComponentInteractionTypeReaderResult>(ObjectId.TryParse(input.ToString(), out var result)
            ? ComponentInteractionTypeReaderResult.Success(result)
            : ComponentInteractionTypeReaderResult.ParseFail(parameter.Name));
    }
}