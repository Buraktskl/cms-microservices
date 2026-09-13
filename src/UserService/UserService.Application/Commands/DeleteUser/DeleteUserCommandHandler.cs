using System.Text.Json;
using MediatR;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Domain.Outbox;

namespace UserService.Application.Commands.DeleteUser;

/// <summary>
/// Implements the Transactional Outbox Pattern for the User Deletion SAGA.
///
/// Problem: Previously, SaveChanges() and PublishEvent() were two separate operations.
/// If the process crashed between them, the DB was updated but no event was published —
/// leaving the system in an inconsistent state (ghost user in ContentService).
///
/// Solution: Write the UserDeletedEvent payload into the OutboxMessages table in the
/// SAME database transaction as the user status update. A background OutboxDispatcher
/// then reads unprocessed messages and publishes them to RabbitMQ, retrying on failure.
/// This guarantees at-least-once delivery without distributed transactions (2PC).
/// </summary>
public class DeleteUserCommandHandler(
    IUserRepository repository,
    IOutboxRepository outbox,
    ICacheService cache)
    : IRequestHandler<DeleteUserCommand>
{
    private const string ListCacheKey = "users:list";
    private static string UserCacheKey(Guid id) => $"user:{id}";

    public async Task Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(command.UserId, ct)
                   ?? throw new UserNotFoundException(command.UserId);

        user.MarkAsPendingDeletion();
        await repository.UpdateAsync(user, ct);

        // Write the outbox message in the SAME unit-of-work as the user update.
        // Both are committed atomically — no event can be lost due to a crash.
        var payload = JsonSerializer.Serialize(new
        {
            UserId = user.Id,
            Username = user.Username,
            CorrelationId = command.CorrelationId,
            OccurredAt = DateTime.UtcNow
        });

        await outbox.AddAsync(OutboxMessage.Create("UserDeleted", payload), ct);

        // Single SaveChanges = one atomic transaction covering both writes.
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(UserCacheKey(command.UserId), ct);
    }
}
