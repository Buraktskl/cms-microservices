using ContentService.Application.DTOs;
using ContentService.Domain.Entities;

namespace ContentService.Application.Common;

public static class ContentMapper
{
    public static ContentDto ToDto(Content content) => new(
        content.Id,
        content.Title,
        content.Body,
        content.AuthorId,
        content.Status.ToString(),
        content.CreatedAt,
        content.UpdatedAt
    );
}
