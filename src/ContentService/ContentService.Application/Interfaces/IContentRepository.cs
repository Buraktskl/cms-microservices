using ContentService.Domain.Entities;

namespace ContentService.Application.Interfaces;

public interface IContentRepository
{
    Task<Content?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Content>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Content>> GetByAuthorIdAsync(Guid authorId, CancellationToken ct = default);
    Task AddAsync(Content content, CancellationToken ct = default);
    Task UpdateAsync(Content content, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
