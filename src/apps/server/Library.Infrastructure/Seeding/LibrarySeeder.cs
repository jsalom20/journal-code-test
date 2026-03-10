using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Seeding;

public static class LibrarySeeder
{
    private static readonly string[] FirstNames =
    [
        "Anna", "Erik", "Maja", "Karl", "Elsa", "Oskar", "Alva", "Hugo", "Siri", "Leo",
        "Astrid", "William", "Elin", "Nils", "Stella", "Felix", "Molly", "Vera", "Liam", "Tilde"
    ];

    private static readonly string[] LastNames =
    [
        "Andersson", "Johansson", "Karlsson", "Nilsson", "Eriksson", "Larsson", "Olsson", "Persson",
        "Svensson", "Gustafsson", "Pettersson", "Jonsson", "Jansson", "Hansson", "Berg", "Lind"
    ];

    public static async Task SeedAsync(LibraryDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.Books.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Catalog seed data is missing. Apply migrations or run scripts/reset-library-db.sh before starting the app.");
        }

        if (await dbContext.BookCopies.AnyAsync(cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var random = new Random(42);
        var borrowers = await dbContext.Borrowers
            .OrderBy(borrower => borrower.CardNumber)
            .ToListAsync(cancellationToken);

        if (borrowers.Count == 0)
        {
            borrowers = Enumerable.Range(1, 100)
                .Select(index =>
                {
                    var firstName = FirstNames[index % FirstNames.Length];
                    var lastName = LastNames[index % LastNames.Length];
                    return new Borrower
                    {
                        CardNumber = $"LIB-{index:0000}",
                        FirstName = firstName,
                        LastName = lastName,
                        Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{index}@library.local",
                        Status = index % 17 == 0 ? BorrowerStatus.Suspended : BorrowerStatus.Active,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-random.Next(30, 500))
                    };
                })
                .ToList();

            dbContext.Borrowers.AddRange(borrowers);
        }

        var books = await dbContext.Books
            .OrderBy(book => book.Title)
            .ToListAsync(cancellationToken);

        var copies = new List<BookCopy>();
        for (var index = 0; index < books.Count; index++)
        {
            var book = books[index];
            var copiesForBook = index < 500 ? 3 : 2;
            for (var copyIndex = 1; copyIndex <= copiesForBook; copyIndex++)
            {
                var sequence = copies.Count + 1;
                var copy = new BookCopy
                {
                    Book = book,
                    Barcode = $"BC-{sequence:000000}",
                    InventoryNumber = $"INV-{sequence:000000}",
                    ShelfLocation = $"A{(index % 12) + 1}-{(copyIndex % 8) + 1}",
                    ConditionStatus = random.Next(0, 20) == 0 ? CopyConditionStatus.Fair : CopyConditionStatus.Good,
                    CirculationStatus = CirculationStatus.Available,
                    AcquiredAtUtc = DateTime.UtcNow.AddDays(-random.Next(30, 2500)),
                    LastInventoryCheckAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 120)),
                    Notes = random.Next(0, 12) == 0 ? "Inspect spine on next inventory pass." : null
                };

                copies.Add(copy);
            }
        }

        dbContext.BookCopies.AddRange(copies);

        var availableCopies = copies.ToList();
        var activeBorrowers = borrowers.Where(borrower => borrower.Status == BorrowerStatus.Active).ToList();

        var activeLoans = new List<Loan>();
        var historicalLoans = new List<Loan>();

        var activeLoanCount = Math.Min(120, availableCopies.Count);
        for (var index = 0; index < activeLoanCount; index++)
        {
            var copy = availableCopies[index];
            copy.CirculationStatus = CirculationStatus.OnLoan;
            copy.ConcurrencyToken = Guid.NewGuid();
            var loan = new Loan
            {
                Copy = copy,
                Borrower = activeBorrowers[index % activeBorrowers.Count],
                CheckedOutAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 16)),
                DueAtUtc = DateTime.UtcNow.AddDays(random.Next(3, 18)),
                CheckoutCondition = copy.ConditionStatus,
                CheckoutNotes = "Seeded active loan."
            };

            activeLoans.Add(loan);
            dbContext.CopyEvents.Add(new CopyEvent
            {
                Copy = copy,
                Borrower = loan.Borrower,
                Loan = loan,
                EventType = CopyEventType.CheckedOut,
                Description = $"{loan.Borrower.FirstName} {loan.Borrower.LastName} checked out this copy."
            });
        }

        var overdueLoanStart = activeLoanCount;
        var overdueLoanEnd = Math.Min(overdueLoanStart + 35, availableCopies.Count);
        for (var index = overdueLoanStart; index < overdueLoanEnd; index++)
        {
            var copy = availableCopies[index];
            copy.CirculationStatus = CirculationStatus.OnLoan;
            copy.ConcurrencyToken = Guid.NewGuid();
            var loan = new Loan
            {
                Copy = copy,
                Borrower = activeBorrowers[index % activeBorrowers.Count],
                CheckedOutAtUtc = DateTime.UtcNow.AddDays(-random.Next(20, 35)),
                DueAtUtc = DateTime.UtcNow.AddDays(-random.Next(2, 15)),
                CheckoutCondition = copy.ConditionStatus,
                CheckoutNotes = "Seeded overdue loan."
            };

            activeLoans.Add(loan);
            dbContext.CopyEvents.Add(new CopyEvent
            {
                Copy = copy,
                Borrower = loan.Borrower,
                Loan = loan,
                EventType = CopyEventType.CheckedOut,
                Description = $"{loan.Borrower.FirstName} {loan.Borrower.LastName} checked out this copy."
            });
        }

        var historicalLoanStart = overdueLoanEnd;
        var historicalLoanEnd = Math.Min(historicalLoanStart + 40, availableCopies.Count);
        for (var index = historicalLoanStart; index < historicalLoanEnd; index++)
        {
            var copy = availableCopies[index];
            var borrower = activeBorrowers[index % activeBorrowers.Count];
            var checkedOutAtUtc = DateTime.UtcNow.AddDays(-random.Next(40, 120));
            var returnedAtUtc = checkedOutAtUtc.AddDays(random.Next(8, 26));
            var loan = new Loan
            {
                Copy = copy,
                Borrower = borrower,
                CheckedOutAtUtc = checkedOutAtUtc,
                DueAtUtc = checkedOutAtUtc.AddDays(21),
                ReturnedAtUtc = returnedAtUtc,
                CheckoutCondition = copy.ConditionStatus,
                ReturnCondition = copy.ConditionStatus,
                CheckoutNotes = "Seeded historical loan.",
                ReturnNotes = "Returned in acceptable condition."
            };

            historicalLoans.Add(loan);
            dbContext.CopyEvents.Add(new CopyEvent
            {
                Copy = copy,
                Borrower = borrower,
                Loan = loan,
                EventType = CopyEventType.CheckedOut,
                Description = $"{borrower.FirstName} {borrower.LastName} checked out this copy."
            });
            dbContext.CopyEvents.Add(new CopyEvent
            {
                Copy = copy,
                Borrower = borrower,
                Loan = loan,
                EventType = CopyEventType.Returned,
                Description = $"{borrower.FirstName} {borrower.LastName} returned this copy.",
                OccurredAtUtc = returnedAtUtc
            });
        }

        dbContext.Loans.AddRange(activeLoans);
        dbContext.Loans.AddRange(historicalLoans);

        var readyReservations = new List<Reservation>();
        var readyReservationStart = historicalLoanEnd;
        var readyReservationCount = Math.Min(15, Math.Max(availableCopies.Count - readyReservationStart, 0));
        for (var index = 0; index < readyReservationCount; index++)
        {
            var copy = availableCopies[readyReservationStart + index];
            copy.CirculationStatus = CirculationStatus.Reserved;
            copy.ConcurrencyToken = Guid.NewGuid();
            var reservation = new Reservation
            {
                BookId = copy.BookId,
                Borrower = activeBorrowers[(index + 20) % activeBorrowers.Count],
                Status = ReservationStatus.ReadyForPickup,
                AssignedCopy = copy,
                QueuedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 6)),
                ReadyForPickupAtUtc = DateTime.UtcNow.AddHours(-random.Next(2, 72))
            };

            readyReservations.Add(reservation);
            dbContext.CopyEvents.Add(new CopyEvent
            {
                Copy = copy,
                Borrower = reservation.Borrower,
                Reservation = reservation,
                EventType = CopyEventType.ReservationAssigned,
                Description = $"{reservation.Borrower.FirstName} {reservation.Borrower.LastName} has a ready reservation."
            });
        }

        var queuedReservations = new List<Reservation>();
        var queuedReservationCount = Math.Min(20, books.Count);
        for (var index = 0; index < queuedReservationCount; index++)
        {
            var sourceBook = books[(index + 40) % books.Count];
            var reservation = new Reservation
            {
                BookId = sourceBook.Id,
                Borrower = activeBorrowers[(index + 40) % activeBorrowers.Count],
                Status = ReservationStatus.Active,
                QueuedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 10))
            };

            queuedReservations.Add(reservation);
        }

        dbContext.Reservations.AddRange(readyReservations);
        dbContext.Reservations.AddRange(queuedReservations);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
