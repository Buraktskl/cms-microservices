using ContentService.Application.DTOs;
using MediatR;

namespace ContentService.Application.Commands.CreateContent;

public record CreateContentCommand(string Title, string Body, Guid AuthorId, string CorrelationId)
    : IRequest<ContentDto>;
