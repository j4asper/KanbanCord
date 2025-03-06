using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Interfaces;
using KanbanCord.Core.Models;

namespace KanbanCord.Bot.Providers;

public class CompletedTaskItemsAutoCompleteProvider : IAutoCompleteProvider
{
    private readonly ITaskItemRepository _repository;

    public CompletedTaskItemsAutoCompleteProvider(ITaskItemRepository repository)
    {
        _repository = repository;
    }
    
    
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var taskItems = await _repository.GetAllTaskItemsByGuildIdAsync(context.Guild!.Id);

        var response = taskItems.GetAutoCompleteStrings(BoardStatus.Completed);
        
        return response.Where(x => context.UserInput == null || x.Name.Contains(context.UserInput, StringComparison.OrdinalIgnoreCase))
            .Take(20);
    }
}