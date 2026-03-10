using Library.Domain.Abstractions;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class Loan : IHasConcurrencyToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CopyId { get; set; }
    public Guid BorrowerId { get; set; }
    public DateTime CheckedOutAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; } = DateTime.UtcNow.AddDays(21);
    public DateTime? ReturnedAtUtc { get; set; }
    public CopyConditionStatus CheckoutCondition { get; set; } = CopyConditionStatus.Good;
    public CopyConditionStatus? ReturnCondition { get; set; }
    public string? CheckoutNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public BookCopy Copy { get; set; } = null!;
    public Borrower Borrower { get; set; } = null!;
}
