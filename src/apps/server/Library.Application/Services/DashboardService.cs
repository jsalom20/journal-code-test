using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Application.Models;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Application.Services;

public sealed class DashboardService
{
    private readonly ILibraryDbContext _dbContext;
    private readonly ISystemClock _clock;

    public DashboardService(ILibraryDbContext dbContext, ISystemClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var totalBooks = await _dbContext.Books.CountAsync(cancellationToken);
        var totalCopies = await _dbContext.BookCopies.CountAsync(cancellationToken);
        var availableCopies = await _dbContext.BookCopies.CountAsync(
            entity => entity.CirculationStatus == CirculationStatus.Available,
            cancellationToken);
        var activeLoans = await _dbContext.Loans.CountAsync(entity => entity.ReturnedAtUtc == null, cancellationToken);
        var overdueLoans = await _dbContext.Loans.CountAsync(
            entity => entity.ReturnedAtUtc == null && entity.DueAtUtc < _clock.UtcNow,
            cancellationToken);
        var activeReservations = await _dbContext.Reservations.CountAsync(
            entity => entity.Status == ReservationStatus.Active || entity.Status == ReservationStatus.ReadyForPickup,
            cancellationToken);
        var suspendedBorrowers = await _dbContext.Borrowers.CountAsync(
            entity => entity.Status == BorrowerStatus.Suspended,
            cancellationToken);

        var activeLoanDueDates = await _dbContext.Loans
            .Where(entity => entity.ReturnedAtUtc == null)
            .Select(entity => entity.DueAtUtc)
            .ToListAsync(cancellationToken);

        var outstandingFines = activeLoanDueDates.Sum(dueAt => FinePolicy.CalculateFine(dueAt, null, _clock.UtcNow));

        return new DashboardSummary(
            totalBooks,
            totalCopies,
            availableCopies,
            activeLoans,
            overdueLoans,
            activeReservations,
            suspendedBorrowers,
            outstandingFines);
    }
}
