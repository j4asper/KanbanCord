using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Providers;

public abstract class TaskAutocompleteProvider : IAutocompleteProvider<AutocompleteInteractionContext>
{
    private const int MaxChoices = 20;

    private readonly IBoardService _boardService;
    private readonly BoardStatus[] _statuses;

    protected TaskAutocompleteProvider(IBoardService boardService, params BoardStatus[] statuses)
    {
        _boardService = boardService;
        _statuses = statuses;
    }

    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        if (context.Interaction.GuildId is not { } guildId)
            return [];

        var columns = await _boardService.GetColumnsAsync(guildId, _statuses);

        return columns
            .SelectMany(column => column.Entries.Select(entry => new
            {
                Name = $"[{column.Status.ToFormattedString()}] {entry.Number} - {entry.Task.Title}",
                Value = entry.Task.Id.ToString(),
            }))
            .Where(choice => string.IsNullOrEmpty(option.Value) || choice.Name.Contains(option.Value, StringComparison.OrdinalIgnoreCase))
            .Take(MaxChoices)
            .Select(choice => new ApplicationCommandOptionChoiceProperties(choice.Name, choice.Value))
            .ToList();
    }
}

public class AllTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed, BoardStatus.Archived);

public class BacklogTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.Backlog);

public class InProgressTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.InProgress);

public class CompletedTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.Completed);
