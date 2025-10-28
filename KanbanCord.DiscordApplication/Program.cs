using KanbanCord.DiscordApplication.Extensions;
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

await app.RunAsync();