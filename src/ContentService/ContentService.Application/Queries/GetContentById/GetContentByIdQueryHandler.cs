using ContentService.Application.Common;
using ContentService.Application.DTOs;
using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;
using MediatR;

namespace ContentService.Application.Queries.GetContentById;

public class GetContentByIdQueryHandler(IContentRepository repository, ICacheService cache)
    : IRequestHandler<GetContentByIdQuery, ContentDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static string CacheKey(Guid id) => $"content:{id}";

    public async Task<ContentDto> Handle(GetContentByIdQuery request, CancellationToken ct)
    {
        var cached = await cache.GetAsync<ContentDto>(CacheKey(request.ContentId), ct);
        if (cached is not null) return cached;

        var content = await repository.GetByIdAsync(request.ContentId, ct)
                      ?? throw new ContentNotFoundException(request.ContentId);

        var dto = ContentMapper.ToDto(content);
        await cache.SetAsync(CacheKey(request.ContentId), dto, CacheTtl, ct);
        return dto;
    }
}
