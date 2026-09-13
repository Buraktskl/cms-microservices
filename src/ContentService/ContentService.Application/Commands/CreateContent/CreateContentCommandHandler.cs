using ContentService.Application.Common;
using ContentService.Application.DTOs;
using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using MediatR;

namespace ContentService.Application.Commands.CreateContent;

public class CreateContentCommandHandler(
    IContentRepository repository,
    IUserServiceClient userServiceClient,
    ICacheService cache)
    : IRequestHandler<CreateContentCommand, ContentDto>
{
    private const string ListCacheKey = "contents:list";

    public async Task<ContentDto> Handle(CreateContentCommand command, CancellationToken ct)
    {
        var userExists = await userServiceClient.UserExistsAsync(command.AuthorId, command.CorrelationId, ct);
        if (!userExists)
            throw new AuthorNotFoundException(command.AuthorId);

        var content = Content.Create(command.Title, command.Body, command.AuthorId);
        await repository.AddAsync(content, ct);
        await repository.SaveChangesAsync(ct);

        await cache.RemoveAsync(ListCacheKey, ct);

        return ContentMapper.ToDto(content);
    }
}
