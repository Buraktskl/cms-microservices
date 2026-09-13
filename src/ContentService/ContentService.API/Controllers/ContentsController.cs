using ContentService.Application.Commands.CreateContent;
using ContentService.Application.Commands.DeleteContent;
using ContentService.Application.Commands.UpdateContent;
using ContentService.Application.DTOs;
using ContentService.Application.Queries.GetAllContents;
using ContentService.Application.Queries.GetContentById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ContentsController(IMediator mediator, ILogger<ContentsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await mediator.Send(new GetAllContentsQuery(), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetContentByIdQuery(id), ct));

    [HttpPost]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] CreateContentRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var command = new CreateContentCommand(request.Title, request.Body, request.AuthorId, correlationId);
        var result = await mediator.Send(command, ct);
        logger.LogInformation("Content created: {ContentId} for author {AuthorId}", result.Id, result.AuthorId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContentRequest request, CancellationToken ct)
    {
        var command = new UpdateContentCommand(id, request.Title, request.Body);
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteContentCommand(id), ct);
        return NoContent();
    }
}
