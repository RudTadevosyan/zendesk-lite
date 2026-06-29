var builder = DistributedApplication.CreateBuilder(args);

// Env
DotNetEnv.Env.Load();
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
             ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured in .env or environment.");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ZendeskLite";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ZendeskLiteUsers";


//  Configure Redis for JWT Refresh Token Management
// After dont forget to make this also with volume as the database
var redis = builder.AddRedis("redis")
    .WithRedisInsight();

// Configure PostgreSQL and define the main database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("zendeskdb")
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
    .WithExternalHttpEndpoints()
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);


// Future Phase Reference: For the workers service 
// builder.AddProject<Projects.ZendeskLite_Worker>("worker")
//     .WithReference(database)
//     .WithReference(rabbitMq);

builder.Build().Run();