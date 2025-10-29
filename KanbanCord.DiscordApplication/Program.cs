using KanbanCord.DiscordApplication.Extensions;
using NetCord.Hosting.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseDefaultServiceProvider()
    .UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration))
    ;

builder.Services.AddHostDependencies();

builder.Services
    .AddHealthChecks()
    .AddCustomHealthChecks()
    ;

var app = builder.Build();

app.AddModules(typeof(Program).Assembly);

app.MapHealthChecks("/health");

await app.RunAsync();