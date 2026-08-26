using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ZendeskLite.Infrastructure.Messaging;

public class MessageBrokerInitializer
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<MessageBrokerInitializer> _logger;

    public MessageBrokerInitializer(IConfiguration configuration, ILogger<MessageBrokerInitializer> logger)
    {
        var connectionString = configuration.GetConnectionString("messaging")
            ?? throw new InvalidOperationException("RabbitMQ connection string 'messaging' not found.");

        _connectionFactory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        // Dead Letter Setup
        await channel.ExchangeDeclareAsync("zendesk.dlx.exchange", ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync("zendesk.dlx.queue", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync("zendesk.dlx.queue", "zendesk.dlx.exchange", routingKey: "ticket.deadletter", cancellationToken: ct);

        // Main Exchange & Queue Setup
        await channel.ExchangeDeclareAsync("zendesk.direct.exchange", ExchangeType.Direct, durable: true, cancellationToken: ct);

        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", "zendesk.dlx.exchange" },
            { "x-dead-letter-routing-key", "ticket.deadletter" }
        };

        await channel.QueueDeclareAsync(
            queue: "zendesk.ticket.queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: ct);

        await channel.QueueBindAsync("zendesk.ticket.queue", "zendesk.direct.exchange", routingKey: "ticket.submitted", cancellationToken: ct);

        _logger.LogInformation("RabbitMQ global broker topology initialized successfully.");
    }
}