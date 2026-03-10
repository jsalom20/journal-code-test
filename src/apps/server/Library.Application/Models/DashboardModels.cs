namespace Library.Application.Models;

public sealed record DashboardSummary(
    int TotalBooks,
    int TotalCopies,
    int AvailableCopies,
    int ActiveLoans,
    int OverdueLoans,
    int ActiveReservations,
    int SuspendedBorrowers,
    decimal OutstandingFinesSek);
