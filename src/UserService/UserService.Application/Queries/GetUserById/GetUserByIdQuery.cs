using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;
