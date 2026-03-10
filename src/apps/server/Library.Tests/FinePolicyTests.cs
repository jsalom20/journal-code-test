using Library.Application.Common;

namespace Library.Tests;

public sealed class FinePolicyTests
{
    [Fact]
    public void CalculateFine_ReturnsZero_WhenLoanIsNotOverdue()
    {
        var dueAt = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var now = dueAt.AddHours(2);

        var fine = FinePolicy.CalculateFine(dueAt, null, now);

        Assert.Equal(0m, fine);
    }

    [Fact]
    public void CalculateFine_CapsAtMaximumFine()
    {
        var dueAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var now = dueAt.AddDays(90);

        var fine = FinePolicy.CalculateFine(dueAt, null, now);

        Assert.Equal(200m, fine);
    }
}
