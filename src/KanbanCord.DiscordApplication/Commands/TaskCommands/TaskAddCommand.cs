using KanbanCord.Core.Constants;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskCommands;

public partial class TaskCommandGroup
{
    [SubSlashCommand("add", "Add a task to the backlog")]
    public InteractionCallbackProperties TaskAddCommand()
    {
        return InteractionCallback.Modal(
            new ModalProperties(
                InteractionIdConstants.TaskAdd.ModalId,
                "Add a new Task",
                [
                        new LabelProperties("Title",
                        new TextInputProperties(InteractionIdConstants.TaskAdd.TitleFieldId, TextInputStyle.Short)
                            .WithMaxLength(Limits.TaskTitleMaxLength)
                            .WithRequired()
                        )
                        .WithDescription($"Give your task a short title (max {Limits.TaskTitleMaxLength} characters)"),
                        new LabelProperties("Description",
                            new TextInputProperties(InteractionIdConstants.TaskAdd.DescriptionFieldId, TextInputStyle.Paragraph)
                                .WithMaxLength(Limits.TaskDescriptionMaxLength)
                                .WithRequired()
                        )
                        .WithDescription($"Give your task a description (max {Limits.TaskDescriptionMaxLength} characters)"),
                ]
            )    
        );
    }
}