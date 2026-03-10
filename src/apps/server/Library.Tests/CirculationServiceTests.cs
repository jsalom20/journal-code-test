using Library.Application.Models;
using Library.Application.Services;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests;

public sealed class CirculationServiceTests
{
    [Fact]
    public async Task CheckoutAsync_Fails_ForSuspendedBorrower()
    {
        await using var scenario = await SeedScenarioAsync();
        scenario.Borrower.Status = BorrowerStatus.Suspended;
        await scenario.DbContext.SaveChangesAsync();

        var service = new CirculationService(scenario.DbContext, scenario.Clock);
        var result = await service.CheckoutAsync(
            new CheckoutRequest(scenario.Copy.Id, scenario.Borrower.Id, CopyConditionStatus.Good, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Suspended borrowers cannot check out books.", result.Error);
    }

    [Fact]
    public async Task CheckoutAsync_UpdatesCopyAndCreatesLoan()
    {
        await using var scenario = await SeedScenarioAsync();
        var service = new CirculationService(scenario.DbContext, scenario.Clock);

        var result = await service.CheckoutAsync(
            new CheckoutRequest(scenario.Copy.Id, scenario.Borrower.Id, CopyConditionStatus.Good, "Desk checkout"),
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var copy = await scenario.DbContext.BookCopies.SingleAsync();
        var loan = await scenario.DbContext.Loans.SingleAsync();

        Assert.Equal(CirculationStatus.OnLoan, copy.CirculationStatus);
        Assert.Equal(scenario.Borrower.Id, loan.BorrowerId);
        Assert.Equal(scenario.Clock.UtcNow.AddDays(21), loan.DueAtUtc);
    }

    [Fact]
    public async Task ReturnAsync_AssignsOldestReservation()
    {
        await using var scenario = await SeedScenarioAsync();
        var service = new CirculationService(scenario.DbContext, scenario.Clock);

        var checkout = await service.CheckoutAsync(
            new CheckoutRequest(scenario.Copy.Id, scenario.Borrower.Id, CopyConditionStatus.Good, null),
            CancellationToken.None);

        var reservationOne = new Reservation
        {
            BookId = scenario.Book.Id,
            BorrowerId = scenario.SecondBorrower.Id,
            QueuedAtUtc = scenario.Clock.UtcNow.AddHours(-3),
            Status = ReservationStatus.Active
        };
        var reservationTwo = new Reservation
        {
            BookId = scenario.Book.Id,
            BorrowerId = scenario.ThirdBorrower.Id,
            QueuedAtUtc = scenario.Clock.UtcNow.AddHours(-1),
            Status = ReservationStatus.Active
        };

        scenario.DbContext.Reservations.AddRange(reservationOne, reservationTwo);
        await scenario.DbContext.SaveChangesAsync();

        var result = await service.ReturnAsync(
            checkout.Value!.LoanId,
            new ReturnLoanRequest(CopyConditionStatus.Good, "Returned"),
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var copy = await scenario.DbContext.BookCopies.SingleAsync();
        var refreshedOne = await scenario.DbContext.Reservations.SingleAsync(entity => entity.Id == reservationOne.Id);
        var refreshedTwo = await scenario.DbContext.Reservations.SingleAsync(entity => entity.Id == reservationTwo.Id);

        Assert.Equal(CirculationStatus.Reserved, copy.CirculationStatus);
        Assert.Equal(ReservationStatus.ReadyForPickup, refreshedOne.Status);
        Assert.Equal(copy.Id, refreshedOne.AssignedCopyId);
        Assert.Equal(ReservationStatus.Active, refreshedTwo.Status);
    }

    [Fact]
    public async Task PlaceReservationAsync_BlocksDuplicateTitleReservationWhenBorrowerHasLoan()
    {
        await using var scenario = await SeedScenarioAsync();
        var service = new CirculationService(scenario.DbContext, scenario.Clock);
        await service.CheckoutAsync(
            new CheckoutRequest(scenario.Copy.Id, scenario.Borrower.Id, CopyConditionStatus.Good, null),
            CancellationToken.None);

        var result = await service.PlaceReservationAsync(
            new CreateReservationRequest(scenario.Book.Id, scenario.Borrower.Id),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Borrower already has this title on loan.", result.Error);
    }

    private static async Task<TestScenario> SeedScenarioAsync()
    {
        var (dbContext, connection) = await TestDbFactory.CreateAsync();
        var clock = new TestClock(new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc));

        var book = new Book
        {
            Title = "The Quiet Archive",
            Isbn13 = "9780000000001",
            Language = "English"
        };

        var copy = new BookCopy
        {
            Book = book,
            Barcode = "BC-000001",
            InventoryNumber = "INV-000001",
            ShelfLocation = "A1-1",
            ConditionStatus = CopyConditionStatus.Good,
            CirculationStatus = CirculationStatus.Available,
            AcquiredAtUtc = clock.UtcNow.AddDays(-120)
        };

        var borrower = new Borrower
        {
            CardNumber = "LIB-0001",
            FirstName = "Anna",
            LastName = "Reader",
            Email = "anna@example.com",
            Status = BorrowerStatus.Active
        };

        var secondBorrower = new Borrower
        {
            CardNumber = "LIB-0002",
            FirstName = "Erik",
            LastName = "Queue",
            Email = "erik@example.com",
            Status = BorrowerStatus.Active
        };

        var thirdBorrower = new Borrower
        {
            CardNumber = "LIB-0003",
            FirstName = "Maja",
            LastName = "Queue",
            Email = "maja@example.com",
            Status = BorrowerStatus.Active
        };

        dbContext.Books.Add(book);
        dbContext.BookCopies.Add(copy);
        dbContext.Borrowers.AddRange(borrower, secondBorrower, thirdBorrower);
        await dbContext.SaveChangesAsync();

        return new TestScenario(dbContext, connection, clock, book, copy, borrower, secondBorrower, thirdBorrower);
    }

    private sealed record TestScenario(
        Library.Infrastructure.Persistence.LibraryDbContext DbContext,
        Microsoft.Data.Sqlite.SqliteConnection Connection,
        TestClock Clock,
        Book Book,
        BookCopy Copy,
        Borrower Borrower,
        Borrower SecondBorrower,
        Borrower ThirdBorrower) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
