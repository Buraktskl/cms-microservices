using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private User() { }

    public static User Create(string username, string email, string fullName)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username.Trim().ToLower(),
            Email = email.Trim().ToLower(),
            FullName = fullName.Trim(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string email, string fullName)
    {
        Email = email.Trim().ToLower();
        FullName = fullName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPendingDeletion()
    {
        Status = UserStatus.PendingDeletion;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        Status = UserStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive() => Status == UserStatus.Active;
}
