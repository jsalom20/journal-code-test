using Library.Application.Abstractions;

namespace Library.Infrastructure.Services;

public sealed class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
