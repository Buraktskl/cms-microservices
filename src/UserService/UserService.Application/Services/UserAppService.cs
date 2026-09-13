using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Application.Services;

public class UserAppService(IUserRepository repository, IUserEventPublisher eventPublisher, ICacheService cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCacheKey = "users:list";
    private static string UserCacheKey(Guid id) => $"user:{id}";

    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<IEnumerable<UserDto>>(ListCacheKey, ct);
        if (cached is not null) return cached;

        var users = await repository.GetAllAsync(ct);
        var dtos = users.Select(MapToDto).ToList();
        await cache.SetAsync(ListCacheKey, dtos, CacheTtl, ct);
        return dtos;
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<UserDto>(UserCacheKey(id), ct);
        if (cached is not null) return cached;

        var user = await repository.GetByIdAsync(id, ct)
                   ?? throw new UserNotFoundException(id);

        var dto = MapToDto(user);
        await cache.SetAsync(UserCacheKey(id), dto, CacheTtl, ct);
        return dto;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await repository.ExistsByUsernameAsync(request.Username, ct))
            throw new UserAlreadyExistsException("username", request.Username);

        if (await repository.ExistsByEmailAsync(request.Email, ct))
            throw new UserAlreadyExistsException("email", request.Email);

        var user = User.Create(request.Username, request.Email, request.FullName);
        await repository.AddAsync(user, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await repository.GetByIdAsync(id, ct)
                   ?? throw new UserNotFoundException(id);

        if (!user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)
            && await repository.ExistsByEmailAsync(request.Email, ct))
            throw new UserAlreadyExistsException("email", request.Email);

        user.Update(request.Email, request.FullName);
        await repository.UpdateAsync(user, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(UserCacheKey(id), ct);

        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id, string correlationId, CancellationToken ct = default)
    {
        var user = await repository.GetByIdAsync(id, ct)
                   ?? throw new UserNotFoundException(id);

        user.MarkAsPendingDeletion();
        await repository.UpdateAsync(user, ct);
        await repository.SaveChangesAsync(ct);

        try
        {
            await eventPublisher.PublishUserDeletedAsync(user.Id, user.Username, correlationId, ct);

            user.MarkAsDeleted();
            await repository.UpdateAsync(user, ct);
            await repository.SaveChangesAsync(ct);

            await cache.RemoveAsync(ListCacheKey, ct);
            await cache.RemoveAsync(UserCacheKey(id), ct);
        }
        catch (Exception ex)
        {
            user.Restore();
            await repository.UpdateAsync(user, ct);
            await repository.SaveChangesAsync(ct);

            throw new UserDeletionFailedException(id, ex.Message);
        }
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FullName,
        user.Status.ToString(),
        user.CreatedAt,
        user.UpdatedAt
    );
}
