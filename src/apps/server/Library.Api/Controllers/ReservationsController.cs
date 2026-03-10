using Library.Application.Models;
using Library.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly CirculationService _circulationService;

    public ReservationsController(CirculationService circulationService)
    {
        _circulationService = circulationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservationListItem>>> GetReservations(
        [FromQuery] Guid? bookId,
        [FromQuery] Guid? borrowerId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var reservations = await _circulationService.GetReservationsAsync(bookId, borrowerId, status, cancellationToken);
        return Ok(reservations);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationListItem>> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _circulationService.PlaceReservationAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetReservations), new { borrowerId = result.Value!.BorrowerId }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{reservationId:guid}")]
    public async Task<IActionResult> DeleteReservation(Guid reservationId, CancellationToken cancellationToken)
    {
        var result = await _circulationService.CancelReservationAsync(reservationId, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
