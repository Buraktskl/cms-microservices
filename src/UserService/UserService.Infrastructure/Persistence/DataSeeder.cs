using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Enums;

namespace UserService.Infrastructure.Persistence;

/// <summary>
/// Populates the database with realistic seed data on first startup.
/// Checks for existing records before inserting to remain idempotent.
/// Uses fixed GUIDs so ContentService seed data can reference the same AuthorIds.
/// </summary>
public static class DataSeeder
{
    // Fixed GUIDs — ContentService seed data references these exact IDs
    public static readonly Guid AliceId   = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
    public static readonly Guid BobId     = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000002");
    public static readonly Guid CarolId   = Guid.Parse("c3d4e5f6-0000-0000-0000-000000000003");
    public static readonly Guid DaveId    = Guid.Parse("d4e5f6a7-0000-0000-0000-000000000004");
    public static readonly Guid EveId     = Guid.Parse("e5f6a7b8-0000-0000-0000-000000000005");

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("[DataSeeder] Users table already has data — skipping seed.");
            return;
        }

        var now = DateTime.UtcNow;

        var users = new[]
        {
            CreateUser(AliceId,  "alice",   "alice@cms.dev",   "Alice Johnson",  now.AddDays(-30)),
            CreateUser(BobId,    "bob",     "bob@cms.dev",     "Bob Martinez",   now.AddDays(-25)),
            CreateUser(CarolId,  "carol",   "carol@cms.dev",   "Carol Thompson", now.AddDays(-20)),
            CreateUser(DaveId,   "dave",    "dave@cms.dev",    "Dave Wilson",    now.AddDays(-15)),
            CreateUser(EveId,    "eve",     "eve@cms.dev",     "Eve Chen",       now.AddDays(-10)),
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        logger.LogInformation("[DataSeeder] Seeded {Count} users.", users.Length);
    }

    private static User CreateUser(Guid id, string username, string email, string fullName, DateTime createdAt)
    {
        // Bypass private constructor via EF's reflection-friendly approach
        var user = (User)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(User));

        typeof(User).GetProperty(nameof(User.Id))!
            .SetValue(user, id);
        typeof(User).GetProperty(nameof(User.Username))!
            .SetValue(user, username);
        typeof(User).GetProperty(nameof(User.Email))!
            .SetValue(user, email);
        typeof(User).GetProperty(nameof(User.FullName))!
            .SetValue(user, fullName);
        typeof(User).GetProperty(nameof(User.Status))!
            .SetValue(user, UserStatus.Active);
        typeof(User).GetProperty(nameof(User.CreatedAt))!
            .SetValue(user, createdAt);
        typeof(User).GetProperty(nameof(User.UpdatedAt))!
            .SetValue(user, createdAt);

        return user;
    }
}
