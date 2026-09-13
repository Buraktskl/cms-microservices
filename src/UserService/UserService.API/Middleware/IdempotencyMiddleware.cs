using System.Text;
using StackExchange.Redis;

namespace UserService.API.Middleware;

public class IdempotencyMiddleware(RequestDelegate next, IConnectionMultiplexer redis, ILogger<IdempotencyMiddleware> logger)
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private static readonly HashSet<string> IdempotentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IdempotentMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[IdempotencyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var redisKey = BuildRedisKey(context, idempotencyKey);
        var db = redis.GetDatabase();

        var cached = await db.StringGetAsync(redisKey);
        if (cached.HasValue)
        {
            logger.LogInformation("Idempotency key {Key} hit — returning cached response", idempotencyKey);
            var cachedEntry = System.Text.Json.JsonSerializer.Deserialize<IdempotencyEntry>(cached!);
            if (cachedEntry is not null)
            {
                context.Response.StatusCode = cachedEntry.StatusCode;
                context.Response.ContentType = cachedEntry.ContentType;
                context.Response.Headers["X-Idempotency-Replayed"] = "true";
                await context.Response.WriteAsync(cachedEntry.Body);
                return;
            }
        }

        // Capture the response
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            // Only cache successful responses
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var entry = new IdempotencyEntry
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/json",
                    Body = responseBody
                };

                var serialized = System.Text.Json.JsonSerializer.Serialize(entry);
                await db.StringSetAsync(redisKey, serialized, Ttl);
                logger.LogInformation("Idempotency key {Key} stored in Redis", idempotencyKey);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static string BuildRedisKey(HttpContext context, string idempotencyKey)
        => $"idempotency:user-service:{context.Request.Method}:{context.Request.Path}:{idempotencyKey}";

    private sealed class IdempotencyEntry
    {
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
