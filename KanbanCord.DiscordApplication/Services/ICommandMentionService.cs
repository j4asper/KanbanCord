namespace KanbanCord.DiscordApplication.Services;

public interface ICommandMentionService
{
    /// <summary>"&lt;/task view:123&gt;" when the command is registered, otherwise "/task view".</summary>
    ValueTask<string> GetMentionAsync(params string[] path);

    ValueTask<string> GetDescriptionAsync(params string[] path);
}
