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
        var copy = await _bookService.CreateCopyAsync(request, cancellationToken);
        return copy is null
            ? NotFound(new { message = "Book not found." })
            : CreatedAtAction(nameof(GetCopies), new { bookId = copy.BookId }, copy);
    }

    [HttpPatch("{copyId:guid}")]
    public async Task<ActionResult<CopyListItem>> UpdateCopy(Guid copyId, [FromBody] CopyUpsertRequest request, CancellationToken cancellationToken)
    {
        var copy = await _bookService.UpdateCopyAsync(copyId, request, cancellationToken);
        return copy is null ? NotFound() : Ok(copy);
    }
}
