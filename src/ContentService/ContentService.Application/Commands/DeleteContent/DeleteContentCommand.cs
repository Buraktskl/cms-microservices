using MediatR;

namespace ContentService.Application.Commands.DeleteContent;

public record DeleteContentCommand(Guid ContentId) : IRequest;
