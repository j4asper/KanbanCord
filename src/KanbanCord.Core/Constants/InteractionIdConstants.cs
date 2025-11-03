namespace KanbanCord.Core.Constants;

public static class InteractionIdConstants
{
    /// <summary>
    /// Interaction Ids related to the "Task Add" command
    /// </summary>
    public static class TaskAdd
    {
        public const string ModalId = "task-add-modal";
        public const string TitleFieldId = "task-add-modal-title-field";
        public const string DescriptionFieldId = "task-add-modal-description-field";
    }

    public static class TaskComment
    {
        /// <summary>
        /// Remember to add the TaskItem Id as a parameter
        /// </summary>
        /// <example>AddCommentButtonId + ":5f92cbf10cf217478ba93561"</example>
        public const string AddCommentButtonId = "task-add-comment-button";
        
        /// <summary>
        /// Remember to add the TaskItem Id as a parameter
        /// </summary>
        /// <example>AddCommentModalId + ":5f92cbf10cf217478ba93561"</example>
        public const string AddCommentModalId = "task-add-comment-modal";
        
        public const string AddCommentModalTextFieldId = "task-add-comment-modal-text-field";
    }

    public static class Task
    {
        /// <summary>
        /// Remember to add the TaskItem Id as a parameter
        /// </summary>
        /// <example>ViewButtonId + ":5f92cbf10cf217478ba93561"</example>
        public const string ViewButtonId = "task-view-button";
    }
}