using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Library.Infrastructure.Persistence;

public sealed class LibraryDbContext : DbContext, ILibraryDbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Borrower> Borrowers => Set<Borrower>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<CopyEvent> CopyEvents => Set<CopyEvent>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(book => book.Title).HasMaxLength(200);
            entity.Property(book => book.Isbn13).HasMaxLength(13);
            entity.Property(book => book.Language).HasMaxLength(50);
            entity.Property(book => book.Publisher).HasMaxLength(120);
            entity.HasIndex(book => book.Title);
            entity.HasIndex(book => book.Isbn13).IsUnique();
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.Property(author => author.Name).HasMaxLength(120);
            entity.HasIndex(author => author.Name).IsUnique();
        });

        modelBuilder.Entity<BookAuthor>(entity =>
        {
            entity.HasKey(link => new { link.BookId, link.AuthorId });
        });

        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.Property(copy => copy.Barcode).HasMaxLength(50);
            entity.Property(copy => copy.InventoryNumber).HasMaxLength(50);
            entity.Property(copy => copy.ShelfLocation).HasMaxLength(50);
            entity.Property(copy => copy.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(copy => copy.Barcode).IsUnique();
            entity.HasIndex(copy => copy.InventoryNumber).IsUnique();
            entity.HasIndex(copy => new { copy.BookId, copy.CirculationStatus });
        });

        modelBuilder.Entity<Borrower>(entity =>
        {
            entity.Property(borrower => borrower.CardNumber).HasMaxLength(20);
            entity.Property(borrower => borrower.FirstName).HasMaxLength(80);
            entity.Property(borrower => borrower.LastName).HasMaxLength(80);
            entity.Property(borrower => borrower.Email).HasMaxLength(160);
            entity.HasIndex(borrower => borrower.CardNumber).IsUnique();
            entity.HasIndex(borrower => borrower.Email).IsUnique();
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.Property(loan => loan.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(loan => new { loan.BorrowerId, loan.ReturnedAtUtc });
            entity.HasIndex(loan => loan.CopyId)
                .IsUnique()
                .HasFilter("\"ReturnedAtUtc\" IS NULL");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(reservation => reservation.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(reservation => new { reservation.BookId, reservation.Status, reservation.QueuedAtUtc });
            entity.HasIndex(reservation => reservation.AssignedCopyId)
                .IsUnique()
                .HasFilter($"\"AssignedCopyId\" IS NOT NULL AND \"Status\" = {(int)ReservationStatus.ReadyForPickup}");
            entity.HasIndex(reservation => new { reservation.BookId, reservation.BorrowerId })
                .IsUnique()
                .HasFilter($"\"Status\" IN ({(int)ReservationStatus.Active}, {(int)ReservationStatus.ReadyForPickup})");
        });

        modelBuilder.Entity<CopyEvent>(entity =>
        {
            entity.Property(copyEvent => copyEvent.Description).HasMaxLength(250);
            entity.HasIndex(copyEvent => new { copyEvent.CopyId, copyEvent.OccurredAtUtc });
        });
    }
}
