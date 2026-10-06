using Microsoft.EntityFrameworkCore;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Infrastructure.Persistence;

namespace ZendeskLite.Worker;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceProvider serviceProvider,
        IMessagePublisher messagePublisher,
        ILogger<OutboxPublisherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var messages = await dbContext.OutboxMessages
                    .Where(x => !x.Processed)
                    .OrderBy(x => x.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        // Count this attempt before publishing.
                        message.Attempts++;

                        await _messagePublisher.PublishRawAsync(
                            message.Id,
                            message.Payload,
                            message.RoutingKey,
                            stoppingToken);

                        message.Processed = true;
                        message.ProcessedAt = DateTime.UtcNow;

                        await dbContext.SaveChangesAsync(
                            stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        await dbContext.SaveChangesAsync(
                            stoppingToken);

                        _logger.LogError(
                            ex,
                            "Failed to publish OutboxMessage {MessageId}. Attempt {Attempt}",
                            message.Id,
                            message.Attempts);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing Outbox messages.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(2),
                stoppingToken);
        }
    }
}