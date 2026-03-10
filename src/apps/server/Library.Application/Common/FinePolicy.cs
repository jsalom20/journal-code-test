namespace Library.Application.Common;

public static class FinePolicy
{
    public const decimal DailyRateSek = 5m;
    public const decimal CapSek = 200m;

    public static decimal CalculateFine(DateTime dueAtUtc, DateTime? returnedAtUtc, DateTime nowUtc)
    {
        var comparisonDate = returnedAtUtc ?? nowUtc;
        if (comparisonDate <= dueAtUtc)
        {
            return 0m;
        }

        var overdueDays = (comparisonDate.Date - dueAtUtc.Date).Days;
        return Math.Min(overdueDays * DailyRateSek, CapSek);
    }
}
