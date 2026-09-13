using ContentService.Application.DTOs;
using MediatR;

namespace ContentService.Application.Queries.GetContentById;

public record GetContentByIdQuery(Guid ContentId) : IRequest<ContentDto>;
