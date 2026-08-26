using ZendeskLite.Infrastructure;
using ZendeskLite.Infrastructure.Messaging;
using ZendeskLite.Infrastructure.Persistence;
using ZendeskLite.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddNpgsqlDbContext<ApplicationDbContext>("zendeskdb");
builder.AddRedisClient("redis");

builder.Services.AddInfrastructure(builder.Configuration);

// register rmq - ticket consumer as a hosted background service
builder.Services.AddHostedService<TicketConsumerWorker>();

var host = builder.Build();

// Initialize the RabbitMQ broker topology before starting the host
using (var scope = host.Services.CreateScope())
{
    var initializer = ActivatorUtilities.CreateInstance<MessageBrokerInitializer>(scope.ServiceProvider);
    await initializer.InitializeAsync();
}

host.Run();