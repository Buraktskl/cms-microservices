using MediatR;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;

namespace UserService.Application.Commands.UpdateUser;

public class UpdateUserCommandHandler(IUserRepository repository, ICacheService cache)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    private const string ListCacheKey = "users:list";
    private static string UserCacheKey(Guid id) => $"user:{id}";

    public async Task<UserDto> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(command.UserId, ct)
                   ?? throw new UserNotFoundException(command.UserId);

        if (!user.Email.Equals(command.Email, StringComparison.OrdinalIgnoreCase)
            && await repository.ExistsByEmailAsync(command.Email, ct))
            throw new UserAlreadyExistsException("email", command.Email);

        user.Update(command.Email, command.FullName);
        await repository.UpdateAsync(user, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(UserCacheKey(command.UserId), ct);

        return UserMapper.ToDto(user);
    }
}
