using ZendeskLite.Infrastructure;
using ZendeskLite.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// 2. Register your RabbitMQ Ticket Consumer as a Hosted Background Service
builder.AddHostedService<TicketConsumerWorker>();

var host = builder.Build();
host.Run();