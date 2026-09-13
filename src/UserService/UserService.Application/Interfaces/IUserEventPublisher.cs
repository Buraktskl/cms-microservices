namespace UserService.Application.Interfaces;

public interface IUserEventPublisher
{
    Task PublishUserDeletedAsync(Guid userId, string username, string correlationId, CancellationToken ct = default);
}
