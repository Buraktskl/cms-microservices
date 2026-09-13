using MediatR;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Application.Commands.CreateUser;

public class CreateUserCommandHandler(IUserRepository repository, ICacheService cache)
    : IRequestHandler<CreateUserCommand, UserDto>
{
    private const string ListCacheKey = "users:list";

    public async Task<UserDto> Handle(CreateUserCommand command, CancellationToken ct)
    {
        if (await repository.ExistsByUsernameAsync(command.Username, ct))
            throw new UserAlreadyExistsException("username", command.Username);

        if (await repository.ExistsByEmailAsync(command.Email, ct))
            throw new UserAlreadyExistsException("email", command.Email);

        var user = User.Create(command.Username, command.Email, command.FullName);
        await repository.AddAsync(user, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);

        return UserMapper.ToDto(user);
    }
}
