using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class CopyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CopyId { get; set; }
    public Guid? LoanId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? BorrowerId { get; set; }
    public CopyEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public BookCopy Copy { get; set; } = null!;
    public Loan? Loan { get; set; }
    public Reservation? Reservation { get; set; }
    public Borrower? Borrower { get; set; }
}
