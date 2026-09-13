using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Common;

public static class UserMapper
{
    public static UserDto ToDto(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FullName,
        user.Status.ToString(),
        user.CreatedAt,
        user.UpdatedAt
    );
}
