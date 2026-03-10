using Library.Application.Services;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Tests.TestSupport;

namespace Library.Tests;

public sealed class BookServiceTests
{
    [Fact]
    public async Task SearchBooksAsync_PaginatesAndFiltersByQuery()
    {
        var (dbContext, connection) = await TestDbFactory.CreateAsync();

        for (var index = 1; index <= 25; index++)
        {
            dbContext.Books.Add(new Book
            {
                Title = index == 8 ? "Patterns of Library Design" : $"Seeded Book {index:00}",
                Isbn13 = $"9780000000{index:000}",
                Language = "English"
            });
        }

        await dbContext.SaveChangesAsync();
        var service = new BookService(dbContext);

        var page = await service.SearchBooksAsync("library", null, 1, 10, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Patterns of Library Design", page.Items[0].Title);

        var secondPage = await service.SearchBooksAsync(null, null, 2, 10, CancellationToken.None);

        Assert.Equal(25, secondPage.TotalCount);
        Assert.Equal(10, secondPage.Items.Count);

        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateCopyAsync_Fails_ForCirculationManagedStatus()
    {
        var (dbContext, connection) = await TestDbFactory.CreateAsync();
        var book = new Book
        {
            Title = "Circulation Rules",
            Isbn13 = "9781000000001",
            Language = "English"
        };

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync();

        var service = new BookService(dbContext);
        var result = await service.CreateCopyAsync(
            new(
                book.Id,
                "BC-100001",
                "INV-100001",
                "A1-1",
                CopyConditionStatus.Good,
                CirculationStatus.OnLoan,
                DateTime.UtcNow,
                null,
                null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Copies cannot be created directly in OnLoan or Reserved status. Use circulation workflows instead.",
            result.Error);

        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task UpdateCopyAsync_Fails_WhenChangingIntoReservedStatusManually()
    {
        var (dbContext, connection) = await TestDbFactory.CreateAsync();
        var book = new Book
        {
            Title = "Manual States",
            Isbn13 = "9781000000002",
            Language = "English"
        };

        var copy = new BookCopy
        {
            Book = book,
            Barcode = "BC-100002",
            InventoryNumber = "INV-100002",
            ShelfLocation = "A1-2",
            ConditionStatus = CopyConditionStatus.Good,
            CirculationStatus = CirculationStatus.Available,
            AcquiredAtUtc = DateTime.UtcNow
        };

        dbContext.BookCopies.Add(copy);
        await dbContext.SaveChangesAsync();

        var service = new BookService(dbContext);
        var result = await service.UpdateCopyAsync(
            copy.Id,
            new(
                book.Id,
                copy.Barcode,
                copy.InventoryNumber,
                copy.ShelfLocation,
                CopyConditionStatus.Good,
                CirculationStatus.Reserved,
                copy.AcquiredAtUtc,
                null,
                null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "OnLoan and Reserved states must be managed through checkout, return, and reservation workflows.",
            result.Error);

        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
