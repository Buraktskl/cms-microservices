using MediatR;

namespace UserService.Application.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId, string CorrelationId) : IRequest;
