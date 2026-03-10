using Library.Domain.Enums;

namespace Library.Application.Models;

public sealed record BorrowerListItem(
    Guid Id,
    string CardNumber,
    string FullName,
    string Email,
    BorrowerStatus Status,
    int ActiveLoansCount,
    int ActiveReservationsCount,
    decimal OutstandingFineSek);

public sealed record BorrowerLoanItem(
    Guid LoanId,
    Guid CopyId,
    Guid BookId,
    string BookTitle,
    string Barcode,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    bool IsOverdue,
    decimal FineSek);

public sealed record BorrowerReservationItem(
    Guid ReservationId,
    Guid BookId,
    string BookTitle,
    ReservationStatus Status,
    DateTime QueuedAtUtc,
    Guid? AssignedCopyId,
    DateTime? ReadyForPickupAtUtc);

public sealed record BorrowerDetail(
    Guid Id,
    string CardNumber,
    string FullName,
    string Email,
    BorrowerStatus Status,
    decimal OutstandingFineSek,
    IReadOnlyList<BorrowerLoanItem> CurrentLoans,
    IReadOnlyList<BorrowerLoanItem> LoanHistory,
    IReadOnlyList<BorrowerReservationItem> Reservations);

public sealed record BorrowerUpsertRequest(
    string CardNumber,
    string FirstName,
    string LastName,
    string Email,
    BorrowerStatus Status);
