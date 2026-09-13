using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Outbox;

/// <summary>
/// Background service that polls the OutboxMessages table and dispatches unprocessed
/// events to RabbitMQ. Runs every 5 seconds, processes up to 20 messages per batch.
///
/// This decouples event publishing from the request lifecycle entirely.
/// Even if RabbitMQ is down at request time, the message is safely persisted in the DB
/// and will be dispatched as soon as the broker recovers.
/// </summary>
public class OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxDispatcher encountered an unexpected error.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IUserEventPublisher>();

        var messages = await outbox.GetUnprocessedAsync(BatchSize, ct);
        if (messages.Count == 0) return;

        logger.LogDebug("OutboxDispatcher processing {Count} messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                if (message.EventType == "UserDeleted")
                {
                    var data = JsonSerializer.Deserialize<UserDeletedPayload>(message.Payload)!;
                    await publisher.PublishUserDeletedAsync(data.UserId, data.Username, data.CorrelationId, ct);
                }

                message.MarkProcessed();
                logger.LogInformation("Outbox message {MessageId} ({EventType}) dispatched.", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                logger.LogWarning(ex, "Outbox message {MessageId} failed (retry {RetryCount}).", message.Id, message.RetryCount);
            }
        }

        await outbox.SaveChangesAsync(ct);
    }

    private record UserDeletedPayload(Guid UserId, string Username, string CorrelationId, DateTime OccurredAt);
}
