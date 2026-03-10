using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Application.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Application.Services;

public sealed class BookService
{
    private readonly ILibraryDbContext _dbContext;

    public BookService(ILibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BookListItem>> SearchBooksAsync(
        string? query,
        string? availability,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var booksQuery = _dbContext.Books
            .AsNoTracking()
            .Include(book => book.BookAuthors)
            .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(book => book.Copies)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            booksQuery = booksQuery.Where(book =>
                book.Title.ToLower().Contains(normalized) ||
                book.Isbn13.Contains(normalized) ||
                book.BookAuthors.Any(author => author.Author.Name.ToLower().Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(availability) &&
            Enum.TryParse<CirculationStatus>(availability, true, out var availabilityStatus))
        {
            booksQuery = booksQuery.Where(book => book.Copies.Any(copy => copy.CirculationStatus == availabilityStatus));
        }
        else if (string.Equals(availability, "available", StringComparison.OrdinalIgnoreCase))
        {
            booksQuery = booksQuery.Where(book => book.Copies.Any(copy => copy.CirculationStatus == CirculationStatus.Available));
        }

        var totalCount = await booksQuery.CountAsync(cancellationToken);
        var items = await booksQuery
            .OrderBy(book => book.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new BookListItem(
                book.Id,
                book.Title,
                book.Isbn13,
                book.Language,
                book.PublicationYear,
                book.Publisher,
                book.BookAuthors.OrderBy(author => author.Author.Name).Select(author => author.Author.Name).ToList(),
                book.Copies.Count,
                book.Copies.Count(copy => copy.CirculationStatus == CirculationStatus.Available),
                book.Copies.Count(copy => copy.CirculationStatus == CirculationStatus.OnLoan),
                book.Copies.Count(copy => copy.CirculationStatus == CirculationStatus.Reserved)))
            .ToListAsync(cancellationToken);

        return new PagedResult<BookListItem>(items, page, pageSize, totalCount);
    }

    public async Task<BookDetail?> GetBookAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _dbContext.Books
            .AsNoTracking()
            .Include(entity => entity.BookAuthors)
            .ThenInclude(entity => entity.Author)
            .Include(entity => entity.Copies)
            .Include(entity => entity.Reservations)
            .ThenInclude(entity => entity.Borrower)
            .FirstOrDefaultAsync(entity => entity.Id == bookId, cancellationToken);

        if (book is null)
        {
            return null;
        }

        var copyIds = book.Copies.Select(copy => copy.Id).ToList();
        var recentEvents = await _dbContext.CopyEvents
            .AsNoTracking()
            .Where(entity => copyIds.Contains(entity.CopyId))
            .Include(entity => entity.Borrower)
            .OrderByDescending(entity => entity.OccurredAtUtc)
            .Take(12)
            .Select(entity => new CopyEventItem(
                entity.Id,
                entity.CopyId,
                entity.EventType,
                entity.Description,
                entity.OccurredAtUtc,
                entity.Borrower == null ? null : $"{entity.Borrower.FirstName} {entity.Borrower.LastName}"))
            .ToListAsync(cancellationToken);

        return new BookDetail(
            book.Id,
            book.Title,
            book.Isbn13,
            book.Language,
            book.PublicationYear,
            book.Publisher,
            book.Summary,
            book.BookAuthors.OrderBy(author => author.Author.Name).Select(author => author.Author.Name).ToList(),
            book.Copies
                .OrderBy(copy => copy.InventoryNumber)
                .Select(copy => new CopySummary(
                    copy.Id,
                    copy.Barcode,
                    copy.InventoryNumber,
                    copy.ShelfLocation,
                    copy.ConditionStatus,
                    copy.CirculationStatus,
                    copy.AcquiredAtUtc,
                    copy.LastInventoryCheckAtUtc,
                    copy.Notes))
                .ToList(),
            book.Reservations
                .Where(reservation => reservation.Status is ReservationStatus.Active or ReservationStatus.ReadyForPickup)
                .OrderBy(reservation => reservation.QueuedAtUtc)
                .Select(reservation => new ReservationQueueItem(
                    reservation.Id,
                    reservation.BorrowerId,
                    $"{reservation.Borrower.FirstName} {reservation.Borrower.LastName}",
                    reservation.Borrower.CardNumber,
                    reservation.Status,
                    reservation.QueuedAtUtc,
                    reservation.AssignedCopyId,
                    reservation.ReadyForPickupAtUtc))
                .ToList(),
            recentEvents);
    }

    public async Task<BookDetail> CreateBookAsync(BookUpsertRequest request, CancellationToken cancellationToken)
    {
        var book = new Book
        {
            Title = request.Title.Trim(),
            Isbn13 = request.Isbn13.Trim(),
            Language = request.Language.Trim(),
            Publisher = request.Publisher?.Trim(),
            Summary = request.Summary?.Trim(),
            PublicationYear = request.PublicationYear,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await AttachAuthorsAsync(book, request.Authors, cancellationToken);
        _dbContext.Books.Add(book);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetBookAsync(book.Id, cancellationToken))!;
    }

    public async Task<BookDetail?> UpdateBookAsync(Guid bookId, BookUpsertRequest request, CancellationToken cancellationToken)
    {
        var book = await _dbContext.Books
            .Include(entity => entity.BookAuthors)
            .FirstOrDefaultAsync(entity => entity.Id == bookId, cancellationToken);

        if (book is null)
        {
            return null;
        }

        book.Title = request.Title.Trim();
        book.Isbn13 = request.Isbn13.Trim();
        book.Language = request.Language.Trim();
        book.Publisher = request.Publisher?.Trim();
        book.Summary = request.Summary?.Trim();
        book.PublicationYear = request.PublicationYear;
        book.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.BookAuthors.RemoveRange(book.BookAuthors);
        book.BookAuthors.Clear();
        await AttachAuthorsAsync(book, request.Authors, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetBookAsync(bookId, cancellationToken);
    }

    public async Task<IReadOnlyList<CopyListItem>> GetCopiesAsync(
        Guid? bookId,
        string? status,
        string? condition,
        CancellationToken cancellationToken)
    {
        var copies = _dbContext.BookCopies
            .AsNoTracking()
            .Include(copy => copy.Book)
            .AsQueryable();

        if (bookId.HasValue)
        {
            copies = copies.Where(copy => copy.BookId == bookId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CirculationStatus>(status, true, out var circulationStatus))
        {
            copies = copies.Where(copy => copy.CirculationStatus == circulationStatus);
        }

        if (!string.IsNullOrWhiteSpace(condition) &&
            Enum.TryParse<CopyConditionStatus>(condition, true, out var conditionStatus))
        {
            copies = copies.Where(copy => copy.ConditionStatus == conditionStatus);
        }

        return await copies
            .OrderBy(copy => copy.Book.Title)
            .ThenBy(copy => copy.InventoryNumber)
            .Select(copy => new CopyListItem(
                copy.Id,
                copy.BookId,
                copy.Book.Title,
                copy.Barcode,
                copy.InventoryNumber,
                copy.ShelfLocation,
                copy.ConditionStatus,
                copy.CirculationStatus,
                copy.AcquiredAtUtc,
                copy.LastInventoryCheckAtUtc,
                copy.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<CopyListItem?> CreateCopyAsync(CopyUpsertRequest request, CancellationToken cancellationToken)
    {
        var book = await _dbContext.Books.FirstOrDefaultAsync(entity => entity.Id == request.BookId, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var copy = new BookCopy
        {
            BookId = request.BookId,
            Barcode = request.Barcode.Trim(),
            InventoryNumber = request.InventoryNumber.Trim(),
            ShelfLocation = request.ShelfLocation.Trim(),
            ConditionStatus = request.ConditionStatus,
            CirculationStatus = request.CirculationStatus,
            AcquiredAtUtc = request.AcquiredAtUtc,
            LastInventoryCheckAtUtc = request.LastInventoryCheckAtUtc,
            Notes = request.Notes?.Trim()
        };

        _dbContext.BookCopies.Add(copy);
        _dbContext.CopyEvents.Add(new CopyEvent
        {
            CopyId = copy.Id,
            EventType = CopyEventType.Created,
            Description = $"Copy {copy.InventoryNumber} created."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCopyAsync(copy.Id, cancellationToken);
    }

    public async Task<CopyListItem?> UpdateCopyAsync(Guid copyId, CopyUpsertRequest request, CancellationToken cancellationToken)
    {
        var copy = await _dbContext.BookCopies
            .FirstOrDefaultAsync(entity => entity.Id == copyId, cancellationToken);

        if (copy is null)
        {
            return null;
        }

        var previousStatus = copy.CirculationStatus;
        var previousCondition = copy.ConditionStatus;

        copy.Barcode = request.Barcode.Trim();
        copy.InventoryNumber = request.InventoryNumber.Trim();
        copy.ShelfLocation = request.ShelfLocation.Trim();
        copy.ConditionStatus = request.ConditionStatus;
        copy.CirculationStatus = request.CirculationStatus;
        copy.AcquiredAtUtc = request.AcquiredAtUtc;
        copy.LastInventoryCheckAtUtc = request.LastInventoryCheckAtUtc;
        copy.Notes = request.Notes?.Trim();
        copy.ConcurrencyToken = Guid.NewGuid();

        if (previousStatus != copy.CirculationStatus)
        {
            _dbContext.CopyEvents.Add(new CopyEvent
            {
                CopyId = copy.Id,
                EventType = CopyEventType.StatusChanged,
                Description = $"Copy status changed from {previousStatus} to {copy.CirculationStatus}."
            });
        }

        if (previousCondition != copy.ConditionStatus)
        {
            _dbContext.CopyEvents.Add(new CopyEvent
            {
                CopyId = copy.Id,
                EventType = CopyEventType.ConditionUpdated,
                Description = $"Copy condition changed from {previousCondition} to {copy.ConditionStatus}."
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCopyAsync(copy.Id, cancellationToken);
    }

    private async Task<CopyListItem?> GetCopyAsync(Guid copyId, CancellationToken cancellationToken)
    {
        return await _dbContext.BookCopies
            .AsNoTracking()
            .Include(copy => copy.Book)
            .Where(copy => copy.Id == copyId)
            .Select(copy => new CopyListItem(
                copy.Id,
                copy.BookId,
                copy.Book.Title,
                copy.Barcode,
                copy.InventoryNumber,
                copy.ShelfLocation,
                copy.ConditionStatus,
                copy.CirculationStatus,
                copy.AcquiredAtUtc,
                copy.LastInventoryCheckAtUtc,
                copy.Notes))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task AttachAuthorsAsync(Book book, IReadOnlyList<string> authorNames, CancellationToken cancellationToken)
    {
        var normalizedNames = authorNames
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var authorName in normalizedNames)
        {
            var existingAuthor = await _dbContext.Authors.FirstOrDefaultAsync(
                entity => entity.Name.ToLower() == authorName.ToLower(),
                cancellationToken);

            var author = existingAuthor ?? new Author { Name = authorName };
            if (existingAuthor is null)
            {
                _dbContext.Authors.Add(author);
            }

            book.BookAuthors.Add(new BookAuthor
            {
                Book = book,
                Author = author
            });
        }
    }
}
