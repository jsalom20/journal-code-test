using Library.Domain.Abstractions;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class BookCopy : IHasConcurrencyToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public string ShelfLocation { get; set; } = string.Empty;
    public CopyConditionStatus ConditionStatus { get; set; } = CopyConditionStatus.Good;
    public CirculationStatus CirculationStatus { get; set; } = CirculationStatus.Available;
    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastInventoryCheckAtUtc { get; set; }
    public string? Notes { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public Book Book { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> AssignedReservations { get; set; } = new List<Reservation>();
    public ICollection<CopyEvent> Events { get; set; } = new List<CopyEvent>();
}
