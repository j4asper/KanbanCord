using KanbanCord.Bot.Extensions;

namespace KanbanCord.Bot;

internal class Program
{
    private static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.Host
            .UseDefaultServiceProvider()
            .UseSerilog();
        
        builder.Services
            .AddHostDependencies()
            .AddDiscordConfiguration(builder.Configuration);

        builder.Services.AddHealthChecks()
            .AddKanbanCordHealthChecks();
        
        var app = builder.Build();

        app.UseHealthChecks("/health");
        
        await app.RunAsync();
    }
}