namespace KanbanCord.DiscordApplication.Components;

public static class InteractionIds
{
    /// <summary>board-refresh:{scope}:{hash}</summary>
    public const string BoardRefresh = "board-refresh";

    public const string TaskAddModal = "task-add-modal";

    /// <summary>task-edit-modal:{taskId}</summary>
    public const string TaskEditModal = "task-edit-modal";

    /// <summary>task-comment:{taskId}</summary>
    public const string TaskCommentButton = "task-comment";

    /// <summary>task-comment-modal:{taskId}</summary>
    public const string TaskCommentModal = "task-comment-modal";

    /// <summary>task-view-page:{taskId}:{page}</summary>
    public const string TaskViewPage = "task-view-page";

    /// <summary>task-delete-confirm:{taskId}</summary>
    public const string TaskDeleteConfirm = "task-delete-confirm";

    public const string TaskDeleteCancel = "task-delete-cancel";

    public const string ClearConfirm = "clear-confirm";
    public const string ClearCancel = "clear-cancel";
    public const string ResetConfirm = "reset-confirm";
    public const string ResetCancel = "reset-cancel";

    /// <summary>Disabled page-label buttons; nothing listens to this id.</summary>
    public const string Noop = "noop";

    public const string TitleField = "titleField";
    public const string DescriptionField = "descriptionField";
    public const string PriorityField = "priorityField";
    public const string CommentField = "commentField";
}
