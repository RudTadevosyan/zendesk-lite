var builder = DistributedApplication.CreateBuilder(args);

//  Configure Redis for JWT Refresh Token Management
var redis = builder.AddRedis("redis")
    .WithRedisInsight();

// Configure PostgreSQL and define the main database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var database = postgres.AddDatabase("zendeskdb");

// Configure RabbitMQ for Async Messaging
var rabbitMq = builder.AddRabbitMQ("messaging");

// Inject dependencies into your Presentation/Web API layer
// The project metadata namespace is auto-generated based on the folder/project name
var webApi = builder.AddProject<Projects.ZendeskLite_API>("webapi")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithExternalHttpEndpoints();

// Future Phase Reference: 
// When you add the background worker in Phase 4, you'll reference them like this:
// builder.AddProject<Projects.ZendeskLite_Worker>("worker")
//     .WithReference(database)
//     .WithReference(rabbitMq);

builder.Build().Run();