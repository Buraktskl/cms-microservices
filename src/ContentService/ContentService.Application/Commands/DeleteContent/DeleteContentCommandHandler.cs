using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;
using MediatR;

namespace ContentService.Application.Commands.DeleteContent;

public class DeleteContentCommandHandler(IContentRepository repository, ICacheService cache)
    : IRequestHandler<DeleteContentCommand>
{
    private const string ListCacheKey = "contents:list";
    private static string ContentCacheKey(Guid id) => $"content:{id}";

    public async Task Handle(DeleteContentCommand command, CancellationToken ct)
    {
        var content = await repository.GetByIdAsync(command.ContentId, ct)
                      ?? throw new ContentNotFoundException(command.ContentId);

        content.SoftDelete();
        await repository.UpdateAsync(content, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);
        await cache.RemoveAsync(ContentCacheKey(command.ContentId), ct);
    }
}
