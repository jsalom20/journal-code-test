using Library.Domain.Abstractions;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class Reservation : IHasConcurrencyToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Guid BorrowerId { get; set; }
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public Guid? AssignedCopyId { get; set; }
    public DateTime? ReadyForPickupAtUtc { get; set; }
    public DateTime? FulfilledAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public Book Book { get; set; } = null!;
    public Borrower Borrower { get; set; } = null!;
    public BookCopy? AssignedCopy { get; set; }
}
