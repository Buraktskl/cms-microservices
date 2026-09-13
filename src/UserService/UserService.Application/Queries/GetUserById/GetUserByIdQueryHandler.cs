using MediatR;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;

namespace UserService.Application.Queries.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository repository, ICacheService cache)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static string CacheKey(Guid id) => $"user:{id}";

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var cached = await cache.GetAsync<UserDto>(CacheKey(request.UserId), ct);
        if (cached is not null) return cached;

        var user = await repository.GetByIdAsync(request.UserId, ct)
                   ?? throw new UserNotFoundException(request.UserId);

        var dto = UserMapper.ToDto(user);
        await cache.SetAsync(CacheKey(request.UserId), dto, CacheTtl, ct);
        return dto;
    }
}
