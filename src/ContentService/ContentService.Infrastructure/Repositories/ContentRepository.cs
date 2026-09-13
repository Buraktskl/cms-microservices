using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Infrastructure.Repositories;

public class ContentRepository(AppDbContext context) : IContentRepository
{
    public async Task<Content?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Contents.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Content>> GetAllAsync(CancellationToken ct = default)
        => await context.Contents.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public async Task<IEnumerable<Content>> GetByAuthorIdAsync(Guid authorId, CancellationToken ct = default)
        => await context.Contents.Where(c => c.AuthorId == authorId).ToListAsync(ct);

    public async Task AddAsync(Content content, CancellationToken ct = default)
        => await context.Contents.AddAsync(content, ct);

    public Task UpdateAsync(Content content, CancellationToken ct = default)
    {
        context.Contents.Update(content);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
