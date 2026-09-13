using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UserService.Application.Interfaces;
using UserService.Application.Queries.GetAllUsers;
using UserService.Infrastructure.Cache;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Outbox;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UserDb")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<ICacheService, RedisCacheService>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "redis:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("RabbitMQ connection string is missing.");

        services.AddSingleton<IUserEventPublisher>(_ =>
            new RabbitMqEventPublisher(rabbitMqConnection));

        // MediatR — scans the Application assembly for all IRequest/IRequestHandler registrations
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<GetAllUsersQuery>());

        // Outbox dispatcher runs as a hosted background service
        services.AddHostedService<OutboxDispatcher>();

        return services;
    }
}
