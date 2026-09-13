using ContentService.Application.DTOs;
using MediatR;

namespace ContentService.Application.Commands.UpdateContent;

public record UpdateContentCommand(Guid ContentId, string Title, string Body)
    : IRequest<ContentDto>;
