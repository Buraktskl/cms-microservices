using ContentService.Application.Common;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using MediatR;

namespace ContentService.Application.Queries.GetAllContents;

public class GetAllContentsQueryHandler(IContentRepository repository, ICacheService cache)
    : IRequestHandler<GetAllContentsQuery, IEnumerable<ContentDto>>
{
    private const string CacheKey = "contents:list";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IEnumerable<ContentDto>> Handle(GetAllContentsQuery request, CancellationToken ct)
    {
        var cached = await cache.GetAsync<IEnumerable<ContentDto>>(CacheKey, ct);
        if (cached is not null) return cached;

        var contents = await repository.GetAllAsync(ct);
        var dtos = contents.Select(ContentMapper.ToDto).ToList();
        await cache.SetAsync(CacheKey, dtos, CacheTtl, ct);
        return dtos;
    }
}
