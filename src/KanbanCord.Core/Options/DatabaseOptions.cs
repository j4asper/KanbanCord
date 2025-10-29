using System.ComponentModel.DataAnnotations;

namespace KanbanCord.Core.Options;

public class DatabaseOptions
{
    public static readonly string Database = nameof(Database);
    
    [Required(AllowEmptyStrings = false)]
    public required string ConnectionString { get; set; }
    
    public string Name { get; set; } = "KanbanCord";
}