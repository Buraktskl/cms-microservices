using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;
