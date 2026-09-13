using ContentService.Application.DTOs;
using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;

namespace ContentService.Application.Services;

public class ContentAppService(IContentRepository repository, IUserServiceClient userServiceClient, ICacheService cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCacheKey = "contents:list";
    private static string ContentCacheKey(Guid id) => $"content:{id}";

    public async Task<IEnumerable<ContentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<IEnumerable<ContentDto>>(ListCacheKey, ct);
        if (cached is not null) return cached;

        var contents = await repository.GetAllAsync(ct);
        var dtos = contents.Select(MapToDto).ToList();
        await cache.SetAsync(ListCacheKey, dtos, CacheTtl, ct);
        return dtos;
    }

    public async Task<ContentDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<ContentDto>(ContentCacheKey(id), ct);
        if (cached is not null) return cached;

        var content = await repository.GetByIdAsync(id, ct)
                      ?? throw new ContentNotFoundException(id);

        var dto = MapToDto(content);
        await cache.SetAsync(ContentCacheKey(id), dto, CacheTtl, ct);
        return dto;
    }

    public async Task<ContentDto> CreateAsync(CreateContentRequest request, string correlationId, CancellationToken ct = default)
    {
        var userExists = await userServiceClient.UserExistsAsync(request.AuthorId, correlationId, ct);

        if (!userExists)
            throw new AuthorNotFoundException(request.AuthorId);

        var content = Content.Create(request.Title, request.Body, request.AuthorId);
        await repository.AddAsync(content, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);

        return MapToDto(content);
    }

    public async Task<ContentDto> UpdateAsync(Guid id, UpdateContentRequest request, CancellationToken ct = default)
    {
        var content = await repository.GetByIdAsync(id, ct)
                      ?? throw new ContentNotFoundException(id);

        content.Update(request.Title, request.Body);
        await repository.UpdateAsync(content, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(ContentCacheKey(id), ct);

        return MapToDto(content);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var content = await repository.GetByIdAsync(id, ct)
                      ?? throw new ContentNotFoundException(id);

        content.SoftDelete();
        await repository.UpdateAsync(content, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(ContentCacheKey(id), ct);
    }

    public async Task DeleteByAuthorAsync(Guid authorId, CancellationToken ct = default)
    {
        var contents = await repository.GetByAuthorIdAsync(authorId, ct);
        foreach (var content in contents)
        {
            content.SoftDelete();
            await repository.UpdateAsync(content, ct);
            await cache.RemoveAsync(ContentCacheKey(content.Id), ct);
        }
        await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync(ListCacheKey, ct);
    }

    private static ContentDto MapToDto(Content content) => new(
        content.Id,
        content.Title,
        content.Body,
        content.AuthorId,
        content.Status.ToString(),
        content.CreatedAt,
        content.UpdatedAt
    );
}
