using KanbanCord.Bot.Extensions;
using Microsoft.Extensions.Hosting;

namespace KanbanCord.Bot;

internal class Program
{
    private static async Task Main()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        
        builder
            .UseConsoleLifetime()
            .AddHostDependencies()
            .UseSerilog();
        
        var app = builder.Build();
        
        await app.RunAsync();
    }
}