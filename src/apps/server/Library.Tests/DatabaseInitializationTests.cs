using Library.Infrastructure;
using Library.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Tests;

public sealed class DatabaseInitializationTests
{
    [Fact]
    public async Task InitializeLibraryDatabaseAsync_SeedsRelativeSqlitePathIntoContentRoot()
    {
        var contentRoot = CreateTempDirectory();
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("ConnectionStrings:LibraryDb", "Data Source=library.dev.db")
                ])
                .Build();

            var services = new ServiceCollection();
            services.AddLibraryInfrastructure(configuration, contentRoot);

            await using var provider = services.BuildServiceProvider();
            await provider.InitializeLibraryDatabaseAsync();

            var expectedPath = Path.Combine(contentRoot, "library.dev.db");
            Assert.True(File.Exists(expectedPath));

            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

            Assert.True(await dbContext.Books.AnyAsync());
            Assert.True(await dbContext.BookCopies.AnyAsync());
            Assert.True(await dbContext.Borrowers.AnyAsync());

            var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.Contains("20260310192854_InitialCreate", appliedMigrations);
            Assert.Contains("20260310210100_SeedCatalogData", appliedMigrations);
            Assert.Contains("20260310224500_AddReadyReservationAssignedCopyIndex", appliedMigrations);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InitializeLibraryDatabaseAsync_ThrowsForLegacySchemaWithoutMigrationHistory()
    {
        var contentRoot = CreateTempDirectory();
        try
        {
            var databasePath = Path.Combine(contentRoot, "legacy.db");
            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE Authors (Id TEXT PRIMARY KEY, Name TEXT NOT NULL);";
                await command.ExecuteNonQueryAsync();
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("ConnectionStrings:LibraryDb", $"Data Source={databasePath}")
                ])
                .Build();

            var services = new ServiceCollection();
            services.AddLibraryInfrastructure(configuration, contentRoot);

            await using var provider = services.BuildServiceProvider();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.InitializeLibraryDatabaseAsync());
            Assert.Contains("scripts/reset-library-db.sh", exception.Message);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"library-init-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
