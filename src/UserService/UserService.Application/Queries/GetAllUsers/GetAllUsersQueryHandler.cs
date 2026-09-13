using MediatR;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Application.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(IUserRepository repository, ICacheService cache)
    : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    private const string CacheKey = "users:list";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var cached = await cache.GetAsync<IEnumerable<UserDto>>(CacheKey, ct);
        if (cached is not null) return cached;

        var users = await repository.GetAllAsync(ct);
        var dtos = users.Select(UserMapper.ToDto).ToList();
        await cache.SetAsync(CacheKey, dtos, CacheTtl, ct);
        return dtos;
    }
}
