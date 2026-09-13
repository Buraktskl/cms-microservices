using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Content> Contents => Set<Content>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).HasMaxLength(500).IsRequired();
            entity.Property(c => c.Body).IsRequired();
            entity.Property(c => c.Status).HasConversion<int>();
            entity.HasIndex(c => c.AuthorId);

            entity.HasQueryFilter(c => c.Status != ContentStatus.Deleted);
        });
    }
}
