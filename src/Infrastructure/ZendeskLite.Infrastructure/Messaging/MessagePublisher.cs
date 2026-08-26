using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
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

        private const string MainExchange = "zendesk.direct.exchange";
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

        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                return;

            _connection = await _connectionFactory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        }

        public async Task PublishAsync<T>(T message, string routingKey, CancellationToken ct = default) where T : class
        {
            await EnsureConnectedAsync(ct);

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