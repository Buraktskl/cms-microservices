using System.Text;
using System.Text.Json;
using ContentService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedContracts.Events;
using SharedContracts.Messaging;

namespace ContentService.Infrastructure.Messaging;

public class UserDeletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserDeletedConsumer> _logger;
    private readonly string _connectionString;
    private IConnection? _connection;
    private IModel? _channel;

    public UserDeletedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<UserDeletedConsumer> logger,
        string connectionString)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _connectionString = connectionString;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(10_000, stoppingToken); // wait for RabbitMQ to be ready

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeNames.CmsEvents, ExchangeType.Topic, durable: true);
            _channel.ExchangeDeclare(ExchangeNames.CmsEventsDlx, ExchangeType.Topic, durable: true);

            _channel.QueueDeclare(
                QueueNames.UserDeletedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", ExchangeNames.CmsEventsDlx },
                    { "x-dead-letter-routing-key", QueueNames.UserDeletedDlq }
                });

            _channel.QueueBind(QueueNames.UserDeletedQueue, ExchangeNames.CmsEvents, RoutingKeys.UserDeleted);
            _channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var @event = JsonSerializer.Deserialize<UserDeletedEvent>(body);
                    if (@event is null)
                    {
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation(
                        "Processing UserDeleted event for UserId {UserId}, CorrelationId {CorrelationId}",
                        @event.UserId, @event.CorrelationId);

                    using var scope = _scopeFactory.CreateScope();
                    var contentService = scope.ServiceProvider.GetRequiredService<ContentAppService>();
                    await contentService.DeleteByAuthorAsync(@event.UserId, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, false);

                    _logger.LogInformation("Successfully processed UserDeleted for UserId {UserId}", @event.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process UserDeleted event. Sending to DLQ.");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(QueueNames.UserDeletedQueue, autoAck: false, consumer);

            _logger.LogInformation("UserDeletedConsumer started, listening on {Queue}", QueueNames.UserDeletedQueue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UserDeletedConsumer stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserDeletedConsumer encountered a fatal error.");
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
