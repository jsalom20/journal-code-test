using Library.Application.Abstractions;
using Library.Application.Services;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Seeding;
using Library.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLibraryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LibraryDb") ?? "Data Source=library.db";

        services.AddDbContext<LibraryDbContext>(options => options.UseSqlite(connectionString));
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
        await dbContext.Database.MigrateAsync(cancellationToken);
        await LibrarySeeder.SeedAsync(dbContext, cancellationToken);
    }
}
