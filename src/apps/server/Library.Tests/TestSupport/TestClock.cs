using Library.Application.Abstractions;

namespace Library.Tests.TestSupport;

internal sealed class TestClock : ISystemClock
{
    public TestClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
