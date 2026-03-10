using Library.Application.Abstractions;
using Library.Application.Services;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Seeding;
using Library.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLibraryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("LibraryDb") ?? "Data Source=library.db";
        var resolvedConnectionString = ResolveSqliteConnectionString(connectionString, contentRootPath);

        services.AddDbContext<LibraryDbContext>(options => options.UseSqlite(resolvedConnectionString));
        services.AddScoped<ILibraryDbContext>(provider => provider.GetRequiredService<LibraryDbContext>());
        services.AddSingleton<ISystemClock, SystemClock>();

        services.AddScoped<BookService>();
        services.AddScoped<BorrowerService>();
        services.AddScoped<CirculationService>();
        services.AddScoped<DashboardService>();

        return services;
    }

    public static async Task InitializeLibraryDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        if (await HasLegacySqliteSchemaWithoutMigrationsAsync(dbContext, cancellationToken))
        {
            throw new InvalidOperationException(
                "The SQLite database already contains tables but no EF migration history. " +
                "Run scripts/reset-library-db.sh to recreate the local database.");
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
        await LibrarySeeder.SeedAsync(dbContext, cancellationToken);
    }

    private static string ResolveSqliteConnectionString(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource) || Path.IsPathRooted(builder.DataSource))
        {
            return builder.ToString();
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        return builder.ToString();
    }

    private static async Task<bool> HasLegacySqliteSchemaWithoutMigrationsAsync(
        LibraryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsSqlite())
        {
            return false;
        }

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (appliedMigrations.Any())
        {
            return false;
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name NOT LIKE 'sqlite_%'
                  AND name NOT IN ('__EFMigrationsHistory', '__EFMigrationsLock');
                """;

            var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return tableCount > 0;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
