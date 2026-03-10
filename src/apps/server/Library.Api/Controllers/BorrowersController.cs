using Library.Application.Models;
using Library.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/borrowers")]
public sealed class BorrowersController : ControllerBase
{
    private readonly BorrowerService _borrowerService;

    public BorrowersController(BorrowerService borrowerService)
    {
        _borrowerService = borrowerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BorrowerListItem>>> GetBorrowers(
        [FromQuery] string? query,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var borrowers = await _borrowerService.GetBorrowersAsync(query, status, cancellationToken);
        return Ok(borrowers);
    }

    [HttpGet("{borrowerId:guid}")]
    public async Task<ActionResult<BorrowerDetail>> GetBorrower(Guid borrowerId, CancellationToken cancellationToken)
    {
        var borrower = await _borrowerService.GetBorrowerAsync(borrowerId, cancellationToken);
        return borrower is null ? NotFound() : Ok(borrower);
    }

    [HttpPost]
    public async Task<ActionResult<BorrowerDetail>> CreateBorrower([FromBody] BorrowerUpsertRequest request, CancellationToken cancellationToken)
    {
        var borrower = await _borrowerService.CreateBorrowerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBorrower), new { borrowerId = borrower.Id }, borrower);
    }

    [HttpPatch("{borrowerId:guid}")]
    public async Task<ActionResult<BorrowerDetail>> UpdateBorrower(Guid borrowerId, [FromBody] BorrowerUpsertRequest request, CancellationToken cancellationToken)
    {
        var borrower = await _borrowerService.UpdateBorrowerAsync(borrowerId, request, cancellationToken);
        return borrower is null ? NotFound() : Ok(borrower);
    }
}
