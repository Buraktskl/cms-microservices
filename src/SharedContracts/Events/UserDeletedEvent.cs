namespace SharedContracts.Events;

public class UserDeletedEvent : BaseEvent
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; } = DateTime.UtcNow;
}
