namespace KanbanCord.Core.Options;

public class UptimeMonitorOptions
{
    public static readonly string UptimeMonitor = nameof(UptimeMonitor);

    public bool Enabled { get; set; } = false;
    
    public required string PushUrl { get; set; }
    
    public required TimeSpan PushInterval { get; set; } = TimeSpan.FromMinutes(1);
}