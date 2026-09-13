using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SharedContracts.Events;
using SharedContracts.Messaging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IUserEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventPublisher(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
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
    }

    public Task PublishUserDeletedAsync(Guid userId, string username, string correlationId, CancellationToken ct = default)
    {
        var @event = new UserDeletedEvent
        {
            UserId = userId,
            Username = username,
            DeletedAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.CorrelationId = correlationId;
        properties.MessageId = @event.EventId.ToString();

        _channel.BasicPublish(
            exchange: ExchangeNames.CmsEvents,
            routingKey: RoutingKeys.UserDeleted,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
