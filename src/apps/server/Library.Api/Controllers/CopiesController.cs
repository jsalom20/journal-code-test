using Library.Application.Models;
using Library.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/copies")]
public sealed class CopiesController : ControllerBase
{
    private readonly BookService _bookService;

    public CopiesController(BookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CopyListItem>>> GetCopies(
        [FromQuery] Guid? bookId,
        [FromQuery] string? status,
        [FromQuery] string? condition,
        CancellationToken cancellationToken)
    {
        var copies = await _bookService.GetCopiesAsync(bookId, status, condition, cancellationToken);
        return Ok(copies);
    }

    [HttpPost]
    public async Task<ActionResult<CopyListItem>> CreateCopy([FromBody] CopyUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.CreateCopyAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetCopies), new { bookId = result.Value!.BookId }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{copyId:guid}")]
    public async Task<ActionResult<CopyListItem>> UpdateCopy(Guid copyId, [FromBody] CopyUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.UpdateCopyAsync(copyId, request, cancellationToken);
        if (!result.Succeeded)
        {
            return string.Equals(result.Error, "Copy not found.", StringComparison.Ordinal)
                ? NotFound()
                : BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }
}
