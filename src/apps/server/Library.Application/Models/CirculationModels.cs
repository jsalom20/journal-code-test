using Library.Domain.Enums;

namespace Library.Application.Models;

public sealed record LoanListItem(
    Guid LoanId,
    Guid BorrowerId,
    string BorrowerName,
    string CardNumber,
    Guid BookId,
    string BookTitle,
    Guid CopyId,
    string Barcode,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    bool IsOverdue,
    decimal FineSek,
    CopyConditionStatus CheckoutCondition,
    CopyConditionStatus? ReturnCondition);

public sealed record CheckoutRequest(
    Guid CopyId,
    Guid BorrowerId,
    CopyConditionStatus CheckoutCondition,
    string? CheckoutNotes);

public sealed record ReturnLoanRequest(
    CopyConditionStatus ReturnCondition,
    string? ReturnNotes);

public sealed record ReservationListItem(
    Guid ReservationId,
    Guid BookId,
    string BookTitle,
    Guid BorrowerId,
    string BorrowerName,
    string CardNumber,
    ReservationStatus Status,
    DateTime QueuedAtUtc,
    Guid? AssignedCopyId,
    DateTime? ReadyForPickupAtUtc);

public sealed record CreateReservationRequest(Guid BookId, Guid BorrowerId);

public sealed record CheckoutResponse(
    Guid LoanId,
    Guid CopyId,
    Guid BorrowerId,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc);

public sealed record ReturnResponse(
    Guid LoanId,
    Guid CopyId,
    CirculationStatus NewCopyStatus,
    Guid? AssignedReservationId);
