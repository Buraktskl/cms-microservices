namespace ContentService.Application.DTOs;

public record ContentDto(
    Guid Id,
    string Title,
    string Body,
    Guid AuthorId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateContentRequest(
    string Title,
    string Body,
    Guid AuthorId
);

public record UpdateContentRequest(
    string Title,
    string Body
);
