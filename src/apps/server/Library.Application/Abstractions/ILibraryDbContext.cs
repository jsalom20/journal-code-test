using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Library.Application.Abstractions;

public interface ILibraryDbContext
{
    DbSet<Book> Books { get; }
    DbSet<Author> Authors { get; }
    DbSet<BookAuthor> BookAuthors { get; }
    DbSet<BookCopy> BookCopies { get; }
    DbSet<Borrower> Borrowers { get; }
    DbSet<Loan> Loans { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<CopyEvent> CopyEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
