using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;

namespace ZendeskLite.Infrastructure.Messaging
{
    public class MessagePublisher : IAsyncDisposable, IMessagePublisher
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private IChannel? _channel;

        // Exchange Names
        private const string MainExchange = "zendesk.direct.exchange";
        private const string DeadLetterExchange = "zendesk.dlx.exchange";

        // Queue Names
        private const string TicketQueue = "zendesk.ticket.queue";
        private const string DeadLetterQueue = "zendesk.dlx.queue";

        private readonly ILogger<MessagePublisher> _logger;

        public MessagePublisher(IConfiguration configuration, ILogger<MessagePublisher> logger)
        {

            var connectionString = configuration.GetConnectionString("messaging")
                                   ?? throw new InvalidOperationException("RabbitMQ connection string 'messaging' not found.");

            _connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };

            _logger = logger;
        }

        private async Task InitializeAsync()
        {
            if ((_connection != null && _connection.IsOpen) && (_channel != null && _channel.IsOpen))
                return;

            _connection = await _connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Main Direct Exchange
            await _channel.ExchangeDeclareAsync(MainExchange, ExchangeType.Direct, durable: true);
            
            // Dead Letter Exchange & Queue 
            await _channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true);
            await _channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: "ticket.deadletter");


            // Declare Main Queue with Dead Letter Arguments pointing to DLX
            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", DeadLetterExchange },
                { "x-dead-letter-routing-key", "ticket.deadletter" }
            };

            await _channel.QueueDeclareAsync(
                queue: TicketQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            // Bind Main Queue to Main Exchange
            await _channel.QueueBindAsync(TicketQueue, MainExchange, routingKey: "ticket.submitted");

            _logger.LogInformation("RabbitMQ topology initialized with DLX safety net.");
        }

        public async Task PublishAsync<T>(T message, string routingKey, CancellationToken ct = default) where T : class
        {
            await InitializeAsync();

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await _channel!.BasicPublishAsync(
                exchange: MainExchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);

            _logger.LogInformation("Published message of type {Type} with routing key {RoutingKey}", typeof(T).Name, routingKey);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }
}
