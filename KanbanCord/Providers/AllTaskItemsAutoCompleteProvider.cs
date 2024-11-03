using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Helpers;
using KanbanCord.Repositories;

namespace KanbanCord.Providers;

public class AllTaskItemsAutoCompleteProvider : IAutoCompleteProvider
{
    private readonly ITaskItemRepository _repository;

    public AllTaskItemsAutoCompleteProvider(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    
    
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var taskItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);

        var response = taskItems.GetAutoCompleteStrings();
        
        return response;
    }
}