namespace UserService.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateUserRequest(
    string Username,
    string Email,
    string FullName
);

public record UpdateUserRequest(
    string Email,
    string FullName
);
