using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Application.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Application.Services;

public sealed class BorrowerService
{
    private readonly ILibraryDbContext _dbContext;
    private readonly ISystemClock _clock;

    public BorrowerService(ILibraryDbContext dbContext, ISystemClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<IReadOnlyList<BorrowerListItem>> GetBorrowersAsync(
        string? query,
        string? status,
        CancellationToken cancellationToken)
    {
        var borrowers = _dbContext.Borrowers
            .AsNoTracking()
            .Include(entity => entity.Loans)
            .Include(entity => entity.Reservations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            borrowers = borrowers.Where(entity =>
                entity.FirstName.ToLower().Contains(normalized) ||
                entity.LastName.ToLower().Contains(normalized) ||
                entity.Email.ToLower().Contains(normalized) ||
                entity.CardNumber.ToLower().Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<BorrowerStatus>(status, true, out var borrowerStatus))
        {
            borrowers = borrowers.Where(entity => entity.Status == borrowerStatus);
        }

        var borrowerEntities = await borrowers
            .OrderBy(entity => entity.LastName)
            .ThenBy(entity => entity.FirstName)
            .ToListAsync(cancellationToken);

        return borrowerEntities
            .Select(entity => new BorrowerListItem(
                entity.Id,
                entity.CardNumber,
                entity.FirstName + " " + entity.LastName,
                entity.Email,
                entity.Status,
                entity.Loans.Count(loan => loan.ReturnedAtUtc == null),
                entity.Reservations.Count(reservation =>
                    reservation.Status == ReservationStatus.Active ||
                    reservation.Status == ReservationStatus.ReadyForPickup),
                entity.Loans
                    .Where(loan => loan.ReturnedAtUtc == null)
                    .Sum(loan => FinePolicy.CalculateFine(loan.DueAtUtc, null, _clock.UtcNow))))
            .ToList();
    }

    public async Task<BorrowerDetail?> GetBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken)
    {
        var borrower = await _dbContext.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == borrowerId, cancellationToken);

        if (borrower is null)
        {
            return null;
        }

        var loans = await _dbContext.Loans
            .AsNoTracking()
            .Where(entity => entity.BorrowerId == borrowerId)
            .Include(entity => entity.Copy)
            .ThenInclude(copy => copy.Book)
            .OrderByDescending(entity => entity.CheckedOutAtUtc)
            .ToListAsync(cancellationToken);

        var reservations = await _dbContext.Reservations
            .AsNoTracking()
            .Where(entity => entity.BorrowerId == borrowerId)
            .Include(entity => entity.Book)
            .OrderByDescending(entity => entity.QueuedAtUtc)
            .ToListAsync(cancellationToken);

        var currentLoans = loans.Where(loan => loan.ReturnedAtUtc == null).Select(ToBorrowerLoanItem).ToList();
        var loanHistory = loans.Where(loan => loan.ReturnedAtUtc != null).Take(20).Select(ToBorrowerLoanItem).ToList();

        return new BorrowerDetail(
            borrower.Id,
            borrower.CardNumber,
            $"{borrower.FirstName} {borrower.LastName}",
            borrower.Email,
            borrower.Status,
            currentLoans.Sum(loan => loan.FineSek),
            currentLoans,
            loanHistory,
            reservations.Select(entity => new BorrowerReservationItem(
                entity.Id,
                entity.BookId,
                entity.Book.Title,
                entity.Status,
                entity.QueuedAtUtc,
                entity.AssignedCopyId,
                entity.ReadyForPickupAtUtc)).ToList());
    }

    public async Task<BorrowerDetail> CreateBorrowerAsync(BorrowerUpsertRequest request, CancellationToken cancellationToken)
    {
        var borrower = new Borrower
        {
            CardNumber = request.CardNumber.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Status = request.Status
        };

        _dbContext.Borrowers.Add(borrower);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBorrowerAsync(borrower.Id, cancellationToken))!;
    }

    public async Task<BorrowerDetail?> UpdateBorrowerAsync(Guid borrowerId, BorrowerUpsertRequest request, CancellationToken cancellationToken)
    {
        var borrower = await _dbContext.Borrowers.FirstOrDefaultAsync(entity => entity.Id == borrowerId, cancellationToken);
        if (borrower is null)
        {
            return null;
        }

        borrower.CardNumber = request.CardNumber.Trim();
        borrower.FirstName = request.FirstName.Trim();
        borrower.LastName = request.LastName.Trim();
        borrower.Email = request.Email.Trim();
        borrower.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetBorrowerAsync(borrowerId, cancellationToken);
    }

    private BorrowerLoanItem ToBorrowerLoanItem(Loan loan)
    {
        var fine = FinePolicy.CalculateFine(loan.DueAtUtc, loan.ReturnedAtUtc, _clock.UtcNow);
        return new BorrowerLoanItem(
            loan.Id,
            loan.CopyId,
            loan.Copy.BookId,
            loan.Copy.Book.Title,
            loan.Copy.Barcode,
            loan.CheckedOutAtUtc,
            loan.DueAtUtc,
            loan.ReturnedAtUtc,
            fine > 0 && loan.ReturnedAtUtc == null,
            fine);
    }
}
