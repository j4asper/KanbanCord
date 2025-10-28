var builder = DistributedApplication.CreateBuilder(args);

var mongodb = builder.AddMongoDB("mongodb")
    .WithMongoExpress(containerName: "express")
    .AddDatabase("database", "KanbanCord")
    ;

builder.AddProject<Projects.KanbanCord_DiscordApplication>("kanbancord")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("Database__ConnectionString", mongodb.Resource.ConnectionStringExpression)
    .WaitFor(mongodb)
    ;

await builder
    .Build()
    .RunAsync();