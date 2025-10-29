var builder = DistributedApplication.CreateBuilder(args);

var mongodb = builder.AddMongoDB("mongodb")
    .WithMongoExpress(containerName: "express")
    .AddDatabase("database", "KanbanCord")
    ;

var botToken = builder.AddParameter("botToken");

builder.AddProject<Projects.KanbanCord_DiscordApplication>("kanbancord")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Database__ConnectionString", mongodb.Resource.ConnectionStringExpression)
    .WithEnvironment("Discord__Token", botToken)
    .WaitFor(mongodb)
    ;

await builder
    .Build()
    .RunAsync();