using KanbanCord.Core.Constants;
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Extensions;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.ComponentHandlers.Modals;

public class TaskAddHandler : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ITaskItemService _taskItemService;

    public TaskAddHandler(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }
    
    [ComponentInteraction(InteractionIdConstants.TaskAdd.ModalId)]
    public async Task<InteractionMessageProperties> InteractionHandler()
    {
        var textInputs = Context.Components
            .OfType<Label>()
            .Select(x => (TextInput)x.Component)
            .ToList();

        var titleField = textInputs
            .First(x => x.CustomId == InteractionIdConstants.TaskAdd.TitleFieldId);            
        var descriptionField = textInputs
            .First(x => x.CustomId == InteractionIdConstants.TaskAdd.DescriptionFieldId);

        var task = new TaskItem
        {
            AuthorId = Context.User.Id,
            GuildId = Context.Guild!.Id,
            Title = titleField.Value,
            Description = descriptionField.Value,
        };
        
        var taskId = await _taskItemService.CreateOrUpdateTaskItemAsync(task);

        return new InteractionMessageProperties()
            .WithFlags(MessageFlags.IsComponentsV2)
            .WithComponents([
                new ComponentContainerProperties([
                    new TextDisplayProperties("A new task has been added to the backlog!"),
                    new ComponentSectionProperties(
                        new ButtonProperties($"{InteractionIdConstants.Task.ViewButtonId}:{taskId}", "View", EmojiProperties.Standard("📋"), ButtonStyle.Primary),
                        [
                            new TextDisplayProperties($"```{task.Title}```")
                        ])
                ])
                .WithDefaultColor()
            ]);
    }
}