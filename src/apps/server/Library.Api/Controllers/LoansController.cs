using Library.Application.Models;
using Library.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/loans")]
public sealed class LoansController : ControllerBase
{
    private readonly CirculationService _circulationService;

    public LoansController(CirculationService circulationService)
    {
        _circulationService = circulationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LoanListItem>>> GetLoans(
        [FromQuery] string? status,
        [FromQuery] Guid? borrowerId,
        CancellationToken cancellationToken)
    {
        var loans = await _circulationService.GetLoansAsync(status, borrowerId, cancellationToken);
        return Ok(loans);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _circulationService.CheckoutAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{loanId:guid}/return")]
    public async Task<ActionResult<ReturnResponse>> Return(Guid loanId, [FromBody] ReturnLoanRequest request, CancellationToken cancellationToken)
    {
        var result = await _circulationService.ReturnAsync(loanId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }
}
