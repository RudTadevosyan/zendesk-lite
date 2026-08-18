using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Worker;

public sealed class TicketConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TicketConsumerWorker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private const string QueueName = "zendesk.ticket.queue";

    public TicketConsumerWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<TicketConsumerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ticket Consumer Worker is starting up...");

        var connectionString = _configuration.GetConnectionString("messaging")
                             ?? throw new InvalidOperationException("RabbitMQ connection string 'messaging' not found.");

        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // process 1 message at a time per worker instance
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                _logger.LogInformation("Successfully connected to RabbitMQ. Listening for tickets...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ is not ready yet. Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        if (_channel == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<TicketSubmittedEvent>(json);

                if (message == null)
                {
                    _logger.LogWarning("Received null or invalid message payload. Rejecting to DLX.");
                    await _channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
                    return;
                }

                _logger.LogInformation("Processing Ticket ID: {TicketId}", message.TicketId);

                // Open an isolated scope because BackgroundService is a Singleton 
                // while EntityFramework DbContext is Scoped.
                using var scope = _serviceProvider.CreateScope();
                var ticketRepository = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();

                // Fetch the raw ticket from database
                var ticket = await ticketRepository.GetByIdAsync(message.TicketId, stoppingToken);
                if (ticket == null)
                {
                    _logger.LogWarning("Ticket {TicketId} not found in database. Acknowledging to clear queue.", message.TicketId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                // ------- Simulate AI Text Optimization & Categorization 
                ticket.Title = "Optimized: " + ticket.Title;
                ticket.CleanedDescription = $"[AI Cleaned]: {ticket.RawDescription}";
                ticket.Category = TicketCategory.Billing;
                ticket.Priority = TicketPriority.High;
                ticket.Status = TicketStatus.UnderReview;

                // ------ Connect to the agent repository for assignment algorithm
                var assignedAgent = await agentRepository.GetBestAvailableAgentAsync(ticket.Category, stoppingToken);
                if (assignedAgent != null)
                {
                    ticket.AgentId = assignedAgent.Id;

                    // Atomically increment in database using just the ID
                    await agentRepository.IncrementActiveLoadAsync(assignedAgent.Id, stoppingToken);

                    _logger.LogInformation("Assigned Ticket {TicketId} to Agent {AgentEmail}",
                        ticket.Id, assignedAgent.Email);
                }
                else
                {
                    _logger.LogWarning("No available agent found for category {Category}. Ticket left unassigned.", ticket.Category);
                }

                await ticketRepository.UpdateAsync(ticket, stoppingToken);

                // Acknowledge successful processing
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully processed and routed Ticket ID: {TicketId}", message.TicketId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming ticket message. Sending to Dead Letter Queue.");
                await _channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}