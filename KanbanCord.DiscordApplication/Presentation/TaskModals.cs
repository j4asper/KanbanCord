using KanbanCord.Core.Constants;
using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Components;
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class TaskModals
{
    public static ModalProperties Add() =>
        new(InteractionIds.TaskAddModal, "Add a new Task", [TitleInput(null), DescriptionInput(null), PriorityInput(Priority.Medium)]);

    /// <summary>Reads the priority chosen in a modal; null when the value is missing or not one of the priorities.</summary>
    public static Priority? ParsePriorityOrNull(string? value) =>
        Enum.TryParse<Priority>(value, out var priority) && Enum.IsDefined(priority) && !int.TryParse(value, out _)
            ? priority
            : null;

    /// <summary>Like <see cref="ParsePriorityOrNull"/>, but a missing or unknown value falls back to Medium (a new task).</summary>
    public static Priority ParsePriority(string? value) => ParsePriorityOrNull(value) ?? Priority.Medium;

    public static ModalProperties Edit(TaskItem task) =>
        new($"{InteractionIds.TaskEditModal}:{task.Id}", "Edit a Task", [TitleInput(task.Title), DescriptionInput(task.Description), PriorityInput(task.Priority ?? Priority.Medium)]);

    public static ModalProperties Comment(string taskId) =>
        new($"{InteractionIds.TaskCommentModal}:{taskId}", "Add a comment to a task",
        [
            new LabelProperties("Comment:",
                new TextInputProperties(InteractionIds.CommentField, TextInputStyle.Paragraph)
                    .WithPlaceholder("Put your comment here")
                    .WithMinLength(Limits.TaskCommentMinLength)
                    .WithMaxLength(Limits.TaskCommentMaxLength)),
        ]);

    private static LabelProperties PriorityInput(Priority selected) =>
        new("Priority:",
            new StringMenuProperties(
                InteractionIds.PriorityField,
                Enum.GetValues<Priority>().Select(priority =>
                    new StringMenuSelectOptionProperties(priority.ToString(), priority.ToString())
                        .WithDefault(priority == selected))));

    private static LabelProperties TitleInput(string? value) =>
        new("Title:",
            new TextInputProperties(InteractionIds.TitleField, TextInputStyle.Short)
                .WithPlaceholder("Title of the task")
                .WithMaxLength(Limits.TaskTitleMaxLength)
                .WithValue(value));

    private static LabelProperties DescriptionInput(string? value) =>
        new("Description:",
            new TextInputProperties(InteractionIds.DescriptionField, TextInputStyle.Paragraph)
                .WithPlaceholder("Description of the task")
                .WithMaxLength(Limits.TaskDescriptionMaxLength)
                .WithValue(value));
}
