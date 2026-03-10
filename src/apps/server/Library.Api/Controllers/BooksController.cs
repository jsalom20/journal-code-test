using Library.Application.Models;
using Library.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController : ControllerBase
{
    private readonly BookService _bookService;

    public BooksController(BookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetBooks(
        [FromQuery] string? query,
        [FromQuery] string? availability,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookService.SearchBooksAsync(query, availability, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{bookId:guid}")]
    public async Task<ActionResult<BookDetail>> GetBook(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _bookService.GetBookAsync(bookId, cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<BookDetail>> CreateBook([FromBody] BookUpsertRequest request, CancellationToken cancellationToken)
    {
        var book = await _bookService.CreateBookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBook), new { bookId = book.Id }, book);
    }

    [HttpPatch("{bookId:guid}")]
    public async Task<ActionResult<BookDetail>> UpdateBook(Guid bookId, [FromBody] BookUpsertRequest request, CancellationToken cancellationToken)
    {
        var book = await _bookService.UpdateBookAsync(bookId, request, cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }
}
