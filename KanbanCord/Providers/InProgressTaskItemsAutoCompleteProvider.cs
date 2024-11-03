using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Helpers;
using KanbanCord.Models;
using KanbanCord.Repositories;

namespace KanbanCord.Providers;

public class InProgressTaskItemsAutoCompleteProvider : IAutoCompleteProvider
{
    private readonly ITaskItemRepository _repository;

    public InProgressTaskItemsAutoCompleteProvider(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    
    
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var taskItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);

        var response = taskItems.GetAutoCompleteStrings(BoardStatus.InProgress);
        
        return response;
    }
}