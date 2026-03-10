using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class Borrower
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CardNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public BorrowerStatus Status { get; set; } = BorrowerStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<CopyEvent> Events { get; set; } = new List<CopyEvent>();
}
