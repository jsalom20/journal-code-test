using Library.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests.TestSupport;

internal static class TestDbFactory
{
    public static async Task<(LibraryDbContext DbContext, SqliteConnection Connection)> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new LibraryDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return (dbContext, connection);
    }
}
