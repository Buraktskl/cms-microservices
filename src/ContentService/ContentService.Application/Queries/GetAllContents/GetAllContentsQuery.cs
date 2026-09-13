using ContentService.Application.DTOs;
using MediatR;

namespace ContentService.Application.Queries.GetAllContents;

public record GetAllContentsQuery : IRequest<IEnumerable<ContentDto>>;
