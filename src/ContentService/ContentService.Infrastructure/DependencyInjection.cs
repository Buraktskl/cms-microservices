using ContentService.Application.Interfaces;
using ContentService.Application.Queries.GetAllContents;
using ContentService.Infrastructure.Cache;
using ContentService.Infrastructure.HttpClients;
using ContentService.Infrastructure.Messaging;
using ContentService.Infrastructure.Persistence;
using ContentService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using StackExchange.Redis;

namespace ContentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ContentDb")));

        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<ICacheService, RedisCacheService>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "redis:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));

        var userServiceBaseUrl = configuration["UserService:BaseUrl"]
            ?? throw new InvalidOperationException("UserService:BaseUrl configuration is missing.");

        services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
        {
            client.BaseAddress = new Uri(userServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddResilienceHandler("user-service-pipeline", pipeline =>
        {
            pipeline.AddTimeout(TimeSpan.FromSeconds(3));

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            });
        });

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("RabbitMQ connection string is missing.");

        services.AddHostedService(sp =>
            new UserDeletedConsumer(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UserDeletedConsumer>>(),
                rabbitMqConnection));

        // MediatR — scans the Application assembly for all IRequest/IRequestHandler registrations
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<GetAllContentsQuery>());

        return services;
    }
}
