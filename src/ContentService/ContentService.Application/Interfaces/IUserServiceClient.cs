namespace ContentService.Application.Interfaces;

public interface IUserServiceClient
{
    /// <summary>
    /// Validates that a user with the given ID exists in UserService.
    /// Returns true if user exists and is active, false if not found.
    /// Throws on communication failure.
    /// </summary>
    Task<bool> UserExistsAsync(Guid userId, string correlationId, CancellationToken ct = default);
}
