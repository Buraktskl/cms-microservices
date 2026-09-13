using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;

namespace ContentService.Infrastructure.Persistence;

/// <summary>
/// Populates the content database with realistic seed data on first startup.
/// AuthorIds match the fixed GUIDs from UserService's DataSeeder — same users, referential sanity.
/// </summary>
public static class DataSeeder
{
    // Must match UserService.Infrastructure.Persistence.DataSeeder fixed GUIDs
    private static readonly Guid AliceId  = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
    private static readonly Guid BobId    = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000002");
    private static readonly Guid CarolId  = Guid.Parse("c3d4e5f6-0000-0000-0000-000000000003");
    private static readonly Guid DaveId   = Guid.Parse("d4e5f6a7-0000-0000-0000-000000000004");
    private static readonly Guid EveId    = Guid.Parse("e5f6a7b8-0000-0000-0000-000000000005");

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Contents.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("[DataSeeder] Contents table already has data — skipping seed.");
            return;
        }

        var now = DateTime.UtcNow;

        var contents = new[]
        {
            // Alice's articles
            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                "Getting Started with Microservices",
                "Microservices architecture breaks a monolithic application into small, independently deployable services. " +
                "Each service owns its data, exposes an API, and communicates via HTTP or messaging. " +
                "This approach enables independent scaling, technology diversity, and fault isolation.",
                AliceId, now.AddDays(-28)),

            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000002"),
                "CQRS Pattern Explained",
                "Command Query Responsibility Segregation separates read and write operations into distinct models. " +
                "Commands mutate state; queries return data. This enables independent optimization of each path, " +
                "cleaner code, and a natural foundation for event sourcing.",
                AliceId, now.AddDays(-22)),

            // Bob's articles
            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000003"),
                "Why Every API Needs Idempotency Keys",
                "Network retries are inevitable. Without idempotency keys, a retry on a payment or order creation " +
                "can result in duplicates. Idempotency keys allow clients to safely retry requests — the server " +
                "returns the cached response for any duplicate submission.",
                BobId, now.AddDays(-20)),

            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000004"),
                "Redis Caching Strategies in .NET",
                "Caching reduces database load and improves response times. Common strategies include cache-aside (lazy loading), " +
                "write-through (update cache on every write), and write-behind (async write). " +
                "This service uses cache-aside with write-through invalidation on mutations.",
                BobId, now.AddDays(-14)),

            // Carol's articles
            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000005"),
                "The Transactional Outbox Pattern",
                "Publishing an event after SaveChanges() creates a reliability gap: if the process crashes between the two operations, " +
                "the database is updated but the event is never sent. The Outbox Pattern writes the event to a DB table " +
                "in the same transaction, then a background dispatcher publishes it — guaranteeing at-least-once delivery.",
                CarolId, now.AddDays(-18)),

            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000006"),
                "Polly Circuit Breaker in Practice",
                "A circuit breaker prevents cascading failures by tracking error rates. When the threshold is exceeded, " +
                "it 'opens' and rejects calls immediately without attempting the downstream request. " +
                "After a recovery window, it enters half-open state and allows a probe request through.",
                CarolId, now.AddDays(-12)),

            // Dave's article
            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000007"),
                "OpenTelemetry: The Observability Standard",
                "OpenTelemetry provides a vendor-neutral SDK for traces, metrics, and logs. " +
                "Instrumenting once lets you export to Jaeger, Datadog, Grafana Tempo, or Honeycomb " +
                "with only a configuration change. This is the observability equivalent of writing against an interface.",
                DaveId, now.AddDays(-8)),

            // Eve's article
            CreateContent(
                Guid.Parse("10000000-0000-0000-0000-000000000008"),
                "CAP Theorem and Real-World Trade-offs",
                "The CAP theorem states a distributed system can guarantee only two of three: Consistency, Availability, Partition Tolerance. " +
                "In practice, partitions are unavoidable, so the real choice is between CP (consistent under partition) " +
                "and AP (available under partition). This service is PA/EL: it prefers availability and accepts eventual consistency.",
                EveId, now.AddDays(-5)),
        };

        db.Contents.AddRange(contents);
        await db.SaveChangesAsync();

        logger.LogInformation("[DataSeeder] Seeded {Count} contents.", contents.Length);
    }

    private static Content CreateContent(Guid id, string title, string body, Guid authorId, DateTime createdAt)
    {
        var content = (Content)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Content));

        typeof(Content).GetProperty(nameof(Content.Id))!
            .SetValue(content, id);
        typeof(Content).GetProperty(nameof(Content.Title))!
            .SetValue(content, title);
        typeof(Content).GetProperty(nameof(Content.Body))!
            .SetValue(content, body);
        typeof(Content).GetProperty(nameof(Content.AuthorId))!
            .SetValue(content, authorId);
        typeof(Content).GetProperty(nameof(Content.Status))!
            .SetValue(content, ContentStatus.Active);
        typeof(Content).GetProperty(nameof(Content.CreatedAt))!
            .SetValue(content, createdAt);
        typeof(Content).GetProperty(nameof(Content.UpdatedAt))!
            .SetValue(content, createdAt);

        return content;
    }
}
