using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Commands.CreateUser;

public record CreateUserCommand(string Username, string Email, string FullName)
    : IRequest<UserDto>;
