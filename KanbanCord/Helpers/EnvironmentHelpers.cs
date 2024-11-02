namespace KanbanCord.Helpers;

public static class EnvironmentHelpers
{
    public static T GetRequired<T>(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException($"Environment variable '{key}' is required, but is missing.");
        
        return (T)Convert.ChangeType(value, typeof(T));
    }
    
    public static T? GetOptionalOrDefault<T>(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(value))
            return default;
        
        return (T)Convert.ChangeType(value, typeof(T));
    }
    
    public static string GetApplicationVersion() => GetOptionalOrDefault<string>("APPLICATION_VERSION") ?? "Unknown";
    
    public static string GetBotToken() => GetRequired<string>("TOKEN");
    
    public static string GetDatabaseConnectionString() => GetRequired<string>("MONGODB_CONNECTION_STRING");
    
    public static string GetDatabaseName() => GetOptionalOrDefault<string>("MONGODB_DATABASE_NAME") ?? "KanbanCord";
    
    public static string? GetSupportServerInvite() => GetOptionalOrDefault<string>("SUPPORT_INVITE");
}