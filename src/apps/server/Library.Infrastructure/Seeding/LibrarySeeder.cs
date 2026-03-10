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

    private static readonly string[] TitlePrefixes =
    [
        "The Silent", "Northbound", "Borrowed", "Last", "Invisible", "Winter", "Golden", "Hidden",
        "Glass", "Midnight", "Paper", "River", "Storm", "Archive", "Library", "Shifting"
    ];

    private static readonly string[] TitleSubjects =
    [
        "Garden", "Letters", "Harbor", "City", "Forest", "Tide", "Memory", "Signal",
        "Bridge", "House", "Map", "Orchard", "Shadow", "Notebook", "Fire", "Compass"
    ];

    private static readonly string[] Publishers =
    [
        "Nordic Press", "Aurora House", "Blue Shelf", "Cedar & Ink", "Granit Books", "Maple Lane"
    ];

    private static readonly string[] Languages =
    [
        "Swedish", "English", "Norwegian", "Danish"
    ];

    public static async Task SeedAsync(LibraryDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Books.AnyAsync(cancellationToken))
        {
            return;
        }

        var random = new Random(42);
        var authors = Enumerable.Range(1, 350)
            .Select(index => new Author { Name = $"Author {index:000}" })
            .ToList();

        dbContext.Authors.AddRange(authors);

        var borrowers = Enumerable.Range(1, 100)
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

        var books = new List<Book>();
        var copies = new List<BookCopy>();

        for (var index = 1; index <= 1000; index++)
        {
            var book = new Book
            {
                Title = $"{TitlePrefixes[index % TitlePrefixes.Length]} {TitleSubjects[(index * 3) % TitleSubjects.Length]}",
                Isbn13 = $"978{index:0000000000}".Substring(0, 13),
                Language = Languages[index % Languages.Length],
                Publisher = Publishers[index % Publishers.Length],
                Summary = $"Catalog summary for seeded book {index}.",
                PublicationYear = 1990 + (index % 35),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-random.Next(100, 1200)),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 90))
            };

            var authorCount = index % 5 == 0 ? 2 : 1;
            var selectedAuthors = authors.Skip(index % authors.Count).Take(authorCount).ToList();
            foreach (var author in selectedAuthors)
            {
                book.BookAuthors.Add(new BookAuthor
                {
                    Book = book,
                    Author = author
                });
            }

            var copiesForBook = index <= 500 ? 3 : 2;
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

                book.Copies.Add(copy);
                copies.Add(copy);
            }

            books.Add(book);
        }

        dbContext.Books.AddRange(books);
        await dbContext.SaveChangesAsync(cancellationToken);

        var availableCopies = copies.ToList();
        var activeBorrowers = borrowers.Where(borrower => borrower.Status == BorrowerStatus.Active).ToList();

        var activeLoans = new List<Loan>();
        var historicalLoans = new List<Loan>();

        for (var index = 0; index < 120; index++)
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

        for (var index = 120; index < 155; index++)
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

        for (var index = 155; index < 195; index++)
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
        for (var index = 0; index < 15; index++)
        {
            var copy = availableCopies[200 + index];
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
        for (var index = 0; index < 20; index++)
        {
            var sourceCopy = availableCopies[40 + index];
            var reservation = new Reservation
            {
                BookId = sourceCopy.BookId,
                Borrower = activeBorrowers[(index + 40) % activeBorrowers.Count],
                Status = ReservationStatus.Active,
                QueuedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 10))
            };

            queuedReservations.Add(reservation);
        }

        dbContext.Reservations.AddRange(readyReservations);
        dbContext.Reservations.AddRange(queuedReservations);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
