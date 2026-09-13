using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Commands.UpdateUser;

public record UpdateUserCommand(Guid UserId, string Email, string FullName)
    : IRequest<UserDto>;
