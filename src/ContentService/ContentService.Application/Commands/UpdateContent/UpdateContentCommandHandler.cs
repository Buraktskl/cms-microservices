using ContentService.Application.Common;
using ContentService.Application.DTOs;
using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;
using MediatR;

namespace ContentService.Application.Commands.UpdateContent;

public class UpdateContentCommandHandler(IContentRepository repository, ICacheService cache)
    : IRequestHandler<UpdateContentCommand, ContentDto>
{
    private const string ListCacheKey = "contents:list";
    private static string ContentCacheKey(Guid id) => $"content:{id}";

    public async Task<ContentDto> Handle(UpdateContentCommand command, CancellationToken ct)
    {
        var content = await repository.GetByIdAsync(command.ContentId, ct)
                      ?? throw new ContentNotFoundException(command.ContentId);

        content.Update(command.Title, command.Body);
        await repository.UpdateAsync(content, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(ContentCacheKey(command.ContentId), ct);

        return ContentMapper.ToDto(content);
    }
}
