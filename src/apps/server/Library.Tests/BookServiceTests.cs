using Library.Application.Services;
using Library.Domain.Entities;
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
}
