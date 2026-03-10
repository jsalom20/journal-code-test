using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Application.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Application.Services;

public sealed class CirculationService
{
    private readonly ILibraryDbContext _dbContext;
    private readonly ISystemClock _clock;

    public CirculationService(ILibraryDbContext dbContext, ISystemClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LoanListItem>> GetLoansAsync(
        string? status,
        Guid? borrowerId,
        CancellationToken cancellationToken)
    {
        var loans = _dbContext.Loans
            .AsNoTracking()
            .Include(entity => entity.Borrower)
            .Include(entity => entity.Copy)
            .ThenInclude(copy => copy.Book)
            .AsQueryable();

        if (borrowerId.HasValue)
        {
            loans = loans.Where(entity => entity.BorrowerId == borrowerId.Value);
        }

        var normalizedStatus = status?.Trim().ToLowerInvariant();
        loans = normalizedStatus switch
        {
            "active" => loans.Where(entity => entity.ReturnedAtUtc == null),
            "overdue" => loans.Where(entity => entity.ReturnedAtUtc == null && entity.DueAtUtc < _clock.UtcNow),
            "history" => loans.Where(entity => entity.ReturnedAtUtc != null),
            _ => loans
        };

        var loanEntities = await loans
            .OrderBy(entity => entity.ReturnedAtUtc == null ? 0 : 1)
            .ThenBy(entity => entity.DueAtUtc)
            .ToListAsync(cancellationToken);

        return loanEntities
            .Select(entity => new LoanListItem(
                entity.Id,
                entity.BorrowerId,
                entity.Borrower.FirstName + " " + entity.Borrower.LastName,
                entity.Borrower.CardNumber,
                entity.Copy.BookId,
                entity.Copy.Book.Title,
                entity.CopyId,
                entity.Copy.Barcode,
                entity.CheckedOutAtUtc,
                entity.DueAtUtc,
                entity.ReturnedAtUtc,
                entity.ReturnedAtUtc == null && entity.DueAtUtc < _clock.UtcNow,
                FinePolicy.CalculateFine(entity.DueAtUtc, entity.ReturnedAtUtc, _clock.UtcNow),
                entity.CheckoutCondition,
                entity.ReturnCondition))
            .ToList();
    }

    public async Task<ServiceResult<CheckoutResponse>> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        var borrower = await _dbContext.Borrowers.FirstOrDefaultAsync(entity => entity.Id == request.BorrowerId, cancellationToken);
        if (borrower is null)
        {
            return ServiceResult<CheckoutResponse>.Failure("Borrower not found.");
        }

        if (borrower.Status == BorrowerStatus.Suspended)
        {
            return ServiceResult<CheckoutResponse>.Failure("Suspended borrowers cannot check out books.");
        }

        var copy = await _dbContext.BookCopies
            .Include(entity => entity.Book)
            .FirstOrDefaultAsync(entity => entity.Id == request.CopyId, cancellationToken);

        if (copy is null)
        {
            return ServiceResult<CheckoutResponse>.Failure("Copy not found.");
        }

        var activeLoanExists = await _dbContext.Loans.AnyAsync(
            entity => entity.CopyId == copy.Id && entity.ReturnedAtUtc == null,
            cancellationToken);

        if (activeLoanExists)
        {
            return ServiceResult<CheckoutResponse>.Failure("This copy is already on loan.");
        }

        var assignedReservation = await _dbContext.Reservations
            .Where(entity => entity.AssignedCopyId == copy.Id && entity.Status == ReservationStatus.ReadyForPickup)
            .OrderBy(entity => entity.QueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (copy.CirculationStatus == CirculationStatus.Reserved &&
            (assignedReservation is null || assignedReservation.BorrowerId != borrower.Id))
        {
            return ServiceResult<CheckoutResponse>.Failure("This copy is reserved for another borrower.");
        }

        if (copy.CirculationStatus != CirculationStatus.Available && copy.CirculationStatus != CirculationStatus.Reserved)
        {
            return ServiceResult<CheckoutResponse>.Failure("Only available or assigned reserved copies can be checked out.");
        }

        var loan = new Loan
        {
            CopyId = copy.Id,
            BorrowerId = borrower.Id,
            CheckedOutAtUtc = _clock.UtcNow,
            DueAtUtc = _clock.UtcNow.AddDays(21),
            CheckoutCondition = request.CheckoutCondition,
            CheckoutNotes = request.CheckoutNotes?.Trim()
        };

        copy.CirculationStatus = CirculationStatus.OnLoan;
        copy.ConditionStatus = request.CheckoutCondition;
        copy.ConcurrencyToken = Guid.NewGuid();

        _dbContext.Loans.Add(loan);
        _dbContext.CopyEvents.Add(new CopyEvent
        {
            CopyId = copy.Id,
            BorrowerId = borrower.Id,
            LoanId = loan.Id,
            EventType = CopyEventType.CheckedOut,
            Description = $"{borrower.FirstName} {borrower.LastName} checked out the copy."
        });

        if (assignedReservation is not null)
        {
            assignedReservation.Status = ReservationStatus.Fulfilled;
            assignedReservation.FulfilledAtUtc = _clock.UtcNow;
            assignedReservation.ConcurrencyToken = Guid.NewGuid();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<CheckoutResponse>.Success(new CheckoutResponse(
            loan.Id,
            loan.CopyId,
            loan.BorrowerId,
            loan.CheckedOutAtUtc,
            loan.DueAtUtc));
    }

    public async Task<ServiceResult<ReturnResponse>> ReturnAsync(Guid loanId, ReturnLoanRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        var loan = await _dbContext.Loans
            .Include(entity => entity.Copy)
            .ThenInclude(copy => copy.Book)
            .Include(entity => entity.Borrower)
            .FirstOrDefaultAsync(entity => entity.Id == loanId, cancellationToken);

        if (loan is null)
        {
            return ServiceResult<ReturnResponse>.Failure("Loan not found.");
        }

        if (loan.ReturnedAtUtc.HasValue)
        {
            return ServiceResult<ReturnResponse>.Failure("This loan has already been returned.");
        }

        loan.ReturnedAtUtc = _clock.UtcNow;
        loan.ReturnCondition = request.ReturnCondition;
        loan.ReturnNotes = request.ReturnNotes?.Trim();
        loan.ConcurrencyToken = Guid.NewGuid();

        var copy = loan.Copy;
        copy.ConditionStatus = request.ReturnCondition;
        copy.ConcurrencyToken = Guid.NewGuid();

        var nextReservation = await _dbContext.Reservations
            .Include(entity => entity.Borrower)
            .Where(entity => entity.BookId == copy.BookId && entity.Status == ReservationStatus.Active)
            .OrderBy(entity => entity.QueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        Guid? assignedReservationId = null;

        if (request.ReturnCondition == CopyConditionStatus.Lost)
        {
            copy.CirculationStatus = CirculationStatus.Lost;
        }
        else if (request.ReturnCondition == CopyConditionStatus.Damaged)
        {
            copy.CirculationStatus = CirculationStatus.Repair;
        }
        else if (nextReservation is not null)
        {
            nextReservation.Status = ReservationStatus.ReadyForPickup;
            nextReservation.AssignedCopyId = copy.Id;
            nextReservation.ReadyForPickupAtUtc = _clock.UtcNow;
            nextReservation.ConcurrencyToken = Guid.NewGuid();
            copy.CirculationStatus = CirculationStatus.Reserved;
            assignedReservationId = nextReservation.Id;

            _dbContext.CopyEvents.Add(new CopyEvent
            {
                CopyId = copy.Id,
                BorrowerId = nextReservation.BorrowerId,
                ReservationId = nextReservation.Id,
                EventType = CopyEventType.ReservationAssigned,
                Description = $"Copy assigned to reservation for {nextReservation.Borrower.FirstName} {nextReservation.Borrower.LastName}."
            });
        }
        else
        {
            copy.CirculationStatus = CirculationStatus.Available;
        }

        _dbContext.CopyEvents.Add(new CopyEvent
        {
            CopyId = copy.Id,
            BorrowerId = loan.BorrowerId,
            LoanId = loan.Id,
            EventType = CopyEventType.Returned,
            Description = $"{loan.Borrower.FirstName} {loan.Borrower.LastName} returned the copy."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<ReturnResponse>.Success(new ReturnResponse(
            loan.Id,
            copy.Id,
            copy.CirculationStatus,
            assignedReservationId));
    }

    public async Task<IReadOnlyList<ReservationListItem>> GetReservationsAsync(
        Guid? bookId,
        Guid? borrowerId,
        string? status,
        CancellationToken cancellationToken)
    {
        var reservations = _dbContext.Reservations
            .AsNoTracking()
            .Include(entity => entity.Book)
            .Include(entity => entity.Borrower)
            .AsQueryable();

        if (bookId.HasValue)
        {
            reservations = reservations.Where(entity => entity.BookId == bookId.Value);
        }

        if (borrowerId.HasValue)
        {
            reservations = reservations.Where(entity => entity.BorrowerId == borrowerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ReservationStatus>(status, true, out var reservationStatus))
        {
            reservations = reservations.Where(entity => entity.Status == reservationStatus);
        }

        return await reservations
            .OrderBy(entity => entity.Status)
            .ThenBy(entity => entity.QueuedAtUtc)
            .Select(entity => new ReservationListItem(
                entity.Id,
                entity.BookId,
                entity.Book.Title,
                entity.BorrowerId,
                entity.Borrower.FirstName + " " + entity.Borrower.LastName,
                entity.Borrower.CardNumber,
                entity.Status,
                entity.QueuedAtUtc,
                entity.AssignedCopyId,
                entity.ReadyForPickupAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<ReservationListItem>> PlaceReservationAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        var borrower = await _dbContext.Borrowers.FirstOrDefaultAsync(entity => entity.Id == request.BorrowerId, cancellationToken);
        if (borrower is null)
        {
            return ServiceResult<ReservationListItem>.Failure("Borrower not found.");
        }

        if (borrower.Status == BorrowerStatus.Suspended)
        {
            return ServiceResult<ReservationListItem>.Failure("Suspended borrowers cannot place reservations.");
        }

        var book = await _dbContext.Books.FirstOrDefaultAsync(entity => entity.Id == request.BookId, cancellationToken);
        if (book is null)
        {
            return ServiceResult<ReservationListItem>.Failure("Book not found.");
        }

        var hasActiveLoan = await _dbContext.Loans
            .Include(entity => entity.Copy)
            .AnyAsync(entity =>
                entity.BorrowerId == request.BorrowerId &&
                entity.ReturnedAtUtc == null &&
                entity.Copy.BookId == request.BookId,
                cancellationToken);

        if (hasActiveLoan)
        {
            return ServiceResult<ReservationListItem>.Failure("Borrower already has this title on loan.");
        }

        var hasActiveReservation = await _dbContext.Reservations.AnyAsync(
            entity =>
                entity.BorrowerId == request.BorrowerId &&
                entity.BookId == request.BookId &&
                (entity.Status == ReservationStatus.Active || entity.Status == ReservationStatus.ReadyForPickup),
            cancellationToken);

        if (hasActiveReservation)
        {
            return ServiceResult<ReservationListItem>.Failure("Borrower already has an active reservation for this title.");
        }

        var reservation = new Reservation
        {
            BookId = request.BookId,
            BorrowerId = request.BorrowerId,
            QueuedAtUtc = _clock.UtcNow,
            Status = ReservationStatus.Active
        };

        var assignableCopy = await _dbContext.BookCopies
            .Where(entity => entity.BookId == request.BookId && entity.CirculationStatus == CirculationStatus.Available)
            .OrderBy(entity => entity.InventoryNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var hasOtherActiveReservations = await _dbContext.Reservations.AnyAsync(
            entity => entity.BookId == request.BookId &&
                      (entity.Status == ReservationStatus.Active || entity.Status == ReservationStatus.ReadyForPickup),
            cancellationToken);

        if (assignableCopy is not null && !hasOtherActiveReservations)
        {
            reservation.Status = ReservationStatus.ReadyForPickup;
            reservation.AssignedCopyId = assignableCopy.Id;
            reservation.ReadyForPickupAtUtc = _clock.UtcNow;
            reservation.ConcurrencyToken = Guid.NewGuid();
            assignableCopy.CirculationStatus = CirculationStatus.Reserved;
            assignableCopy.ConcurrencyToken = Guid.NewGuid();
        }

        _dbContext.Reservations.Add(reservation);
        if (reservation.AssignedCopyId.HasValue)
        {
            _dbContext.CopyEvents.Add(new CopyEvent
            {
                CopyId = reservation.AssignedCopyId.Value,
                BorrowerId = borrower.Id,
                ReservationId = reservation.Id,
                EventType = CopyEventType.ReservationPlaced,
                Description = $"{borrower.FirstName} {borrower.LastName} placed a reservation for {book.Title}."
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<ReservationListItem>.Success(new ReservationListItem(
            reservation.Id,
            reservation.BookId,
            book.Title,
            borrower.Id,
            $"{borrower.FirstName} {borrower.LastName}",
            borrower.CardNumber,
            reservation.Status,
            reservation.QueuedAtUtc,
            reservation.AssignedCopyId,
            reservation.ReadyForPickupAtUtc));
    }

    public async Task<ServiceResult<bool>> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        var reservation = await _dbContext.Reservations
            .Include(entity => entity.Book)
            .Include(entity => entity.Borrower)
            .FirstOrDefaultAsync(entity => entity.Id == reservationId, cancellationToken);

        if (reservation is null)
        {
            return ServiceResult<bool>.Failure("Reservation not found.");
        }

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Fulfilled)
        {
            return ServiceResult<bool>.Failure("Reservation can no longer be cancelled.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAtUtc = _clock.UtcNow;
        reservation.ConcurrencyToken = Guid.NewGuid();

        if (reservation.AssignedCopyId.HasValue)
        {
            var assignedCopy = await _dbContext.BookCopies.FirstAsync(entity => entity.Id == reservation.AssignedCopyId.Value, cancellationToken);
            var nextReservation = await _dbContext.Reservations
                .Include(entity => entity.Borrower)
                .Where(entity =>
                    entity.BookId == reservation.BookId &&
                    entity.Id != reservation.Id &&
                    entity.Status == ReservationStatus.Active)
                .OrderBy(entity => entity.QueuedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextReservation is not null)
            {
                nextReservation.Status = ReservationStatus.ReadyForPickup;
                nextReservation.AssignedCopyId = assignedCopy.Id;
                nextReservation.ReadyForPickupAtUtc = _clock.UtcNow;
                nextReservation.ConcurrencyToken = Guid.NewGuid();
                assignedCopy.CirculationStatus = CirculationStatus.Reserved;
                assignedCopy.ConcurrencyToken = Guid.NewGuid();

                _dbContext.CopyEvents.Add(new CopyEvent
                {
                    CopyId = assignedCopy.Id,
                    BorrowerId = nextReservation.BorrowerId,
                    ReservationId = nextReservation.Id,
                    EventType = CopyEventType.ReservationAssigned,
                    Description = $"Copy reassigned to {nextReservation.Borrower.FirstName} {nextReservation.Borrower.LastName} after a cancellation."
                });
            }
            else
            {
                assignedCopy.CirculationStatus = CirculationStatus.Available;
                assignedCopy.ConcurrencyToken = Guid.NewGuid();
            }
        }

        if (reservation.AssignedCopyId.HasValue)
        {
            _dbContext.CopyEvents.Add(new CopyEvent
            {
                CopyId = reservation.AssignedCopyId.Value,
                BorrowerId = reservation.BorrowerId,
                ReservationId = reservation.Id,
                EventType = CopyEventType.ReservationCancelled,
                Description = $"{reservation.Borrower.FirstName} {reservation.Borrower.LastName} cancelled their reservation."
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
