using ContentService.Domain.Enums;

namespace ContentService.Domain.Entities;

public class Content
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public ContentStatus Status { get; private set; } = ContentStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Content() { }

    public static Content Create(string title, string body, Guid authorId)
    {
        return new Content
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Body = body.Trim(),
            AuthorId = authorId,
            Status = ContentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string body)
    {
        Title = title.Trim();
        Body = body.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        Status = ContentStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive() => Status == ContentStatus.Active;
}
