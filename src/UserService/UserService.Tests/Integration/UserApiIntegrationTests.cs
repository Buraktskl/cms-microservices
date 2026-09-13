using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using UserService.Application.DTOs;
using UserService.Infrastructure.Persistence;

namespace UserService.Tests.Integration;

/// <summary>
/// Integration tests that spin up real PostgreSQL and Redis containers via TestContainers.
/// These tests verify the full request → handler → repository → database roundtrip,
/// unlike unit tests which mock all dependencies.
///
/// Pattern: Each test class implements IAsyncLifetime to start/stop containers around the test suite.
/// WebApplicationFactory replaces the real DB/Redis connection strings with TestContainer ones.
/// </summary>
public class UserApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("user_test_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Testing");
                host.ConfigureServices(services =>
                {
                    // Replace real DB with TestContainer PostgreSQL
                    var dbDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (dbDescriptor is not null) services.Remove(dbDescriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));

                    // Override Redis connection string with TestContainer Redis
                    var redisDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer));
                    if (redisDescriptor is not null) services.Remove(redisDescriptor);

                    services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                        StackExchange.Redis.ConnectionMultiplexer.Connect(_redis.GetConnectionString()));
                });
            });

        // Run migrations against the test database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WithCorrectPayload()
    {
        var request = new CreateUserRequest("integrationuser", "integration@test.com", "Integration User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<UserDto>();
        created.Should().NotBeNull();
        created!.Username.Should().Be("integrationuser");
        created.Email.Should().Be("integration@test.com");
    }

    [Fact]
    public async Task GetUserById_AfterCreate_ReturnsUser()
    {
        var request = new CreateUserRequest("getbyiduser", "getbyid@test.com", "GetById User");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", request);
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var getResponse = await _client.GetAsync($"/api/v1/users/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<UserDto>();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_Returns409Conflict()
    {
        var request = new CreateUserRequest("duplicateuser", "original@test.com", "Original");
        await _client.PostAsJsonAsync("/api/v1/users", request);

        var duplicate = new CreateUserRequest("duplicateuser", "different@test.com", "Duplicate");
        var response = await _client.PostAsJsonAsync("/api/v1/users", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetUser_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IdempotencyKey_DuplicateRequest_ReturnsSameResponse()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateUserRequest("idempotentuser", "idempotent@test.com", "Idempotent User");

        var firstResponse = await SendWithIdempotencyKey(request, idempotencyKey);
        var secondResponse = await SendWithIdempotencyKey(request, idempotencyKey);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var first = await firstResponse.Content.ReadFromJsonAsync<UserDto>();
        var second = await secondResponse.Content.ReadFromJsonAsync<UserDto>();
        second!.Id.Should().Be(first!.Id);
    }

    private async Task<HttpResponseMessage> SendWithIdempotencyKey(CreateUserRequest request, string key)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", key);
        return await _client.SendAsync(httpRequest);
    }
}
