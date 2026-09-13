namespace SharedContracts.Events;

public class ContentCreatedEvent : BaseEvent
{
    public Guid ContentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid AuthorId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
