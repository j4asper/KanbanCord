using System.ComponentModel.DataAnnotations;

namespace KanbanCord.Core.Options;

public class DiscordOptions
{
    public static readonly string Discord = nameof(Discord);
    
    [Required(AllowEmptyStrings = false)]
    public required string Token { get; set; }
    
    public string? SupportInvite { get; set; } = null;
}