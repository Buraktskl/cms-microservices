namespace UserService.Domain.Outbox;

/// <summary>
/// Represents a message stored transactionally in the database before being dispatched to the message broker.
/// This implements the Transactional Outbox Pattern, guaranteeing at-least-once delivery
/// even if the broker is temporarily unavailable.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload) =>
        new() { EventType = eventType, Payload = payload };

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
