namespace KanbanCord.Core.Options;

public class KanbanCordOptions
{
    public static readonly string KanbanCord = nameof(KanbanCord);
    
    public string? SupportInvite { get; set; } = null;

    public string? GetSupportInvite() => string.IsNullOrWhiteSpace(SupportInvite) ? null : SupportInvite.Trim();
}
